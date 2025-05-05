using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using SkiaSharp;
using System.Data;
//using System.Data.SqlClient;
using static LIP.Program.Graphe;
using MySql.Data.MySqlClient;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;
using System.Text.Encodings.Web;

namespace LIP
{
    internal class Program
    {

        public static class XmlHelper
        {
            public static void Save<T>(IEnumerable<T> items, string path)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var xs = new XmlSerializer(typeof(List<T>));
                using var fs = File.Create(path);
                xs.Serialize(fs, items.ToList());
            }

            public static List<T> Load<T>(string path)
            {
                var xs = new XmlSerializer(typeof(List<T>));
                using var fs = File.OpenRead(path);
                return (List<T>)xs.Deserialize(fs)!;
            }
        }
        public static class JsonHelper
        {
            private static readonly JsonSerializerOptions Opts = new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // 2️⃣
            };

            public static void Save<T>(IEnumerable<T> items, string path)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(items, Opts));    // 3️⃣
            }

            public static List<T> Load<T>(string path)
                => JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), Opts)!;
        }
        public static void AfficherChemin(Dictionary<int, int> precedent, int depart, int arrivee, List<string> nomsGares, int[,] matricePoids = null)
        {
            var chemin = new List<int>();
            int current = arrivee;
            int poidsTotal = 0;

            while (current != depart)
            {
                chemin.Add(current);

                if (!precedent.ContainsKey(current))
                {
                    Console.WriteLine("Aucun chemin");
                    return;
                }

                int pred = precedent[current];

                
                if (matricePoids != null)
                {
                    poidsTotal += matricePoids[pred, current];
                }

                current = pred;
            }

            chemin.Add(depart);
            chemin.Reverse();

            Console.WriteLine("Chemin trouvé :");
            for (int i = 0; i < chemin.Count; i++)
            {
                int id = chemin[i];
                string nom = (nomsGares != null && id <= nomsGares.Count)
                    ? nomsGares[id - 1]
                    : $"Gare {id}";

                Console.Write($"-> {nom}");
                if (i < chemin.Count - 1) Console.WriteLine();
            }

            
            if (matricePoids != null)
            {
                Console.WriteLine($"\n Temps total : {poidsTotal} min");
            }

            Console.WriteLine();
        }


        public class Noeud
        {
            public int Numéro { get; set; }
            public List<int> Connexion { get; set; } = new List<int>();
        }

        public class Lien
        {
            public int N1 { get; set; }
            public int N2 { get; set; }
            public int Poids { get; set; } = 1;
        }
        public class Chemin
        {
            public int Sommet { get; set; }
            public int Distance { get; set; }
            public List<int> Predecesseurs { get; set; } = new List<int>();
        }

        public class Graphe
        {
            public List<Noeud> L_Adjacence { get; set; } = new List<Noeud>();
            public int[,] M_Adjacence { get; set; }
            public int N_Noeuds { get; set; }

            public Graphe(int n_Noeuds, List<Lien> liens)
            {
                N_Noeuds = n_Noeuds;
                M_Adjacence = new int[n_Noeuds + 1, n_Noeuds + 1];

                for (int i = 1; i <= n_Noeuds; i++)
                {
                    L_Adjacence.Add(new Noeud { Numéro = i });
                }

                foreach (var lien in liens)
                {
                    L_Adjacence[lien.N1 - 1].Connexion.Add(lien.N2);
                    L_Adjacence[lien.N2 - 1].Connexion.Add(lien.N1);

                    M_Adjacence[lien.N1, lien.N2] = lien.Poids;
                    M_Adjacence[lien.N2, lien.N1] = lien.Poids;
                }
            }

            public int NombreDeCouleurs()
            {
                

                var noeuds = L_Adjacence.OrderByDescending(n => n.Connexion.Count).ToList();

                int[] couleurs = new int[N_Noeuds];
                for (int i = 0; i < N_Noeuds; i++)
                {
                    couleurs[i] = -1; 
                }

                for (int i = 0; i < N_Noeuds; i++)
                {
                    int noeud = noeuds[i].Numéro - 1;
                    var voisins = L_Adjacence[noeud].Connexion;
                    var couleursVoisins = voisins.Select(v => couleurs[v - 1]).Where(c => c != -1).ToList();

                    
                    int couleur = 0;
                    while (couleursVoisins.Contains(couleur))
                    {
                        couleur++;
                    }

                    couleurs[noeud] = couleur;
                }

                
                return couleurs.Distinct().Count();
            }

            // Dictionnaire : clé = Numéro du nœud (1‑based), valeur = indice de couleur (0,1,2…)
            public Dictionary<int, int> AssocierCouleurs()
            {
                // 1. Ordonne les nœuds du plus haut au plus faible degré (heuristique gloutonne)
                var noeuds = L_Adjacence
                             .OrderByDescending(n => n.Connexion.Count)
                             .ToList();

                // 2. Tableau temporaire des couleurs (–1 = non colorié)
                int[] couleurs = Enumerable.Repeat(-1, N_Noeuds).ToArray();

                // 3. Parcours des nœuds dans l’ordre choisi
                foreach (var n in noeuds)
                {
                    int indexNoeud = n.Numéro - 1;                     // Passe à 0‑based
                    var voisins = L_Adjacence[indexNoeud].Connexion;

                    // Couleurs déjà prises par les voisins
                    var couleursVoisins = voisins
                                          .Select(v => couleurs[v - 1])
                                          .Where(c => c != -1)
                                          .ToHashSet();                // HashSet = Contains O(1)

                    // 4. Choisit la première couleur libre
                    int couleur = 0;
                    while (couleursVoisins.Contains(couleur))
                        couleur++;

                    couleurs[indexNoeud] = couleur;                    // Attribue
                }

                // 5. Transforme le tableau en dictionnaire (clé = nœud, valeur = couleur)
                var resultat = Enumerable.Range(0, N_Noeuds)
                                         .ToDictionary(i => i + 1,   // remet en 1‑based
                                                       i => couleurs[i]);

                return resultat;
            }

            public bool EstBiparti()
            {
                int couleursNecessaires = NombreDeCouleurs();
                if (couleursNecessaires == 2)
                {
                    Console.WriteLine("Biparti n=2");
                    return true;
                }
                else 
                {
                    Console.WriteLine("Pas Biparti n!=2");
                    return false;
                }
                
            }

            
            public bool EstPlanaire()
            {
                int couleursNecessaires = NombreDeCouleurs();
                if (couleursNecessaires <= 4)
                {
                    Console.WriteLine("Planaire n<=4");
                    return true;
                }
                else 
                {
                    Console.WriteLine("Pas Planaire n>4");
                    return false;
                }
                
            }


            public void Afficher_L_Adjacence()
            {
                foreach (var noeud in L_Adjacence)
                {
                    Console.Write($"{noeud.Numéro}: ");
                    Console.WriteLine(string.Join(", ", noeud.Connexion));
                }
            }

            public void Afficher_M_Adjacence()
            {
                for (int i = 1; i <= N_Noeuds; i++)
                {
                    for (int j = 1; j <= N_Noeuds; j++)
                    {
                        Console.Write(M_Adjacence[i, j] + " ");
                    }
                    Console.WriteLine();
                }
            }

            public void Dijkstra(string nomDepart, string nomArrivee, List<string> nomsGares)
            {
                int idDepart = nomsGares.FindIndex(n => n.Equals(nomDepart, StringComparison.OrdinalIgnoreCase)) + 1;
                int idArrivee = nomsGares.FindIndex(n => n.Equals(nomArrivee, StringComparison.OrdinalIgnoreCase)) + 1;

                if (idDepart <= 0 || idArrivee <= 0)
                {
                    Console.WriteLine( "Une des deux gares non existente");
                    return;
                
                }

                var distances  = new Dictionary<int, int>();
                var precedents =  new Dictionary<int, int>();
                var nonVisites =  new HashSet<int>();

                
                for ( int i = 1; i <= N_Noeuds; i++)
                {
                    distances[i]  = 99999;
                    nonVisites.Add(i );
                }



                distances[idDepart] =  0;
                 
                while (  nonVisites.Count > 0)
                {
                    
                    int N_actuel  = nonVisites.OrderBy(n => distances[n]).First();
                    nonVisites.Remove(N_actuel );

                    
                    foreach (var voisin in L_Adjacence[N_actuel - 1].Connexion)
                    {
                        if (!nonVisites.Contains(voisin)) 
                        {  
                            continue; 
                        }
                             

                        int poidsLien = M_Adjacence[N_actuel, voisin];
                        int distancePotentielle = distances[N_actuel] + poidsLien;

                        if (distancePotentielle  < distances[voisin])
                        {
                            distances[voisin] =  distancePotentielle;
                            precedents[voisin]  = N_actuel;
                        }
                    } 

                }


                Console.WriteLine($"\n[ Dijkstra ]  {nomDepart} -> {nomArrivee}");
                Program.AfficherChemin( precedents, idDepart, idArrivee, nomsGares, M_Adjacence);
            }


            public void BellmanFord(  string nomDepart, string nomArrivee, List<string> nomsGares)
            {
                int idDepart  = nomsGares.FindIndex(n => n.Equals(nomDepart, StringComparison.OrdinalIgnoreCase)) + 1;
                int idArrivee =  nomsGares.FindIndex(n => n.Equals(nomArrivee, StringComparison.OrdinalIgnoreCase)) + 1;

                if (idDepart <= 0 ||  idArrivee <= 0)
                {
                    Console.WriteLine( "Impossible de trouver une des gares.");
                    return;
                }

                var distances = new  Dictionary<int, int>();
                var precedents = new  Dictionary<int, int>();

                
                for (int i = 1; i <=  N_Noeuds; i++)
                    distances[i] =  99999;

                distances[idDepart] = 0;

                
                for (int i = 0; i <  N_Noeuds - 1; i++)
                {
                    foreach (var noeud  in L_Adjacence)
                    {
                        int u =  noeud.Numéro;

                         foreach (var v in noeud.Connexion)
                        {
                             int poids = M_Adjacence[u, v];

                             if (distances[u] != 99999 && distances[u] + poids < distances[v])
                            {
                                distances[v] =  distances[u] + poids;
                                precedents[v]  = u;
                            }
                        }

                    }
                    

                }


                 
                foreach (var  noeud in L_Adjacence)
                {
                    int u =  noeud.Numéro;

                    foreach (var  v in noeud.Connexion)
                    {
                        int poids =  M_Adjacence[u, v];

                        if (distances[u]  != 99999 && distances[u] + poids < distances[v])
                        {
                             Console.WriteLine("Cycle ---.");
                             return;

                        }
                    }

                }


                 Console.WriteLine($"\n[ Bellman-Ford ] {nomDepart} -> {nomArrivee}");
                Program.AfficherChemin(precedents,  idDepart, idArrivee, nomsGares, M_Adjacence);
            }

            public void FloydWarshall( string nomDepart, string nomArrivee, List<string> nomsGares)
            {
                const int INF = 99999 ;
                 int n = N_Noeuds;

                 int[,] dist = new int[n + 1, n + 1];
                 int[,] suivant = new int[n + 1,  n + 1];
                                 
                for ( int i = 1; i <= n; i++)
                {
                    
                    for (int j = 1; j <= n; j++)
                    {
                        
                        if (i == j)
                        {
                            dist[i,  j] = 0;
                            suivant[i , j] = j;
                        }
                       
                        else if (M_Adjacence[i,  j] > 0)
                        {
                            dist[i,  j] = M_Adjacence[i, j];
                            suivant[ i, j] = j;
                        }
                       
                        else
                        {
                            dist[i, j] = INF;
                            suivant[i, j] = -1;
                        }
                    }
                }

             
                 for (int  k = 1; k <= n; k++)
                {

                    for ( int i = 1; i <= n; i++)
                    {

                        for (int j = 1; j <= n; j++)
                        {

                            if (dist[ i, k] + dist[k, j] < dist[i, j])
                            {
                                dist[ i, j] = dist[i, k] + dist[k, j];
                                suivant[i,  j] = suivant[i, k];
                            }
                        }
                    }
                }

                 
                int idDepart  = nomsGares.FindIndex(nom => nom.Equals(nomDepart, StringComparison.OrdinalIgnoreCase)) + 1;
                int idArrivee =  nomsGares.FindIndex(nom => nom.Equals(nomArrivee, StringComparison.OrdinalIgnoreCase)) + 1;

                if (idDepart <= 0 || idArrivee <= 0 || suivant[idDepart, idArrivee] == -1)
                {
                    Console.WriteLine(" Pas de chemin possible entre ces deux gares.");
                    return;
                }

                 
                List<int> chemin =  new List<int> { idDepart };
                int actuel  = idDepart;

                while (actuel  != idArrivee)
                {
                    actuel =  suivant[actuel, idArrivee];
                    chemin.Add( actuel);
                }

               
                Console.WriteLine($"\n[ Floyd-Warshall ]  {nomDepart} → {nomArrivee}");
                int poidsTotal  = 0;

                for (int i = 0;  i <  chemin.Count; i++)
                {
                    int id  = chemin[i];
                    string nom =  nomsGares[id - 1];
                     Console.Write($"-> {nom}");
                    if (i < chemin.Count - 1) Console.WriteLine();

                    if  (i < chemin.Count - 1)
                        poidsTotal  = poidsTotal+ M_Adjacence[chemin[i], chemin[i + 1]];
                }

                Console.WriteLine($"\nTemps total : {poidsTotal} min");
            }



            public class StatistiquesRepository
            {
                private readonly string connectionString;

                public StatistiquesRepository(string connectionString)
                {
                    this.connectionString = connectionString;
                }

                // 1. Cuisiniers avec plus de livraisons que la moyenne
                public void AfficherCuisiniersAuDessusDeLaMoyenne()
                {
                    string sql = @"
SELECT cu.idCuisinier, COUNT(DISTINCT l.idLivraison) AS NombreLivraisons
FROM Cuisinier cu
JOIN Effectue e ON cu.idCuisinier = e.idCuisinier
JOIN Livraison l ON e.idLivraison = l.idLivraison
GROUP BY cu.idCuisinier
HAVING COUNT(DISTINCT l.idLivraison) > (
    SELECT AVG(Nombre)
    FROM (
        SELECT COUNT(DISTINCT l.idLivraison) AS Nombre
        FROM Cuisinier cu
        JOIN Effectue e ON cu.idCuisinier = e.idCuisinier
        JOIN Livraison l ON e.idLivraison = l.idLivraison
        GROUP BY cu.idCuisinier
    ) AS Moyennes
)";
                    using var conn = new MySqlConnection(connectionString);
                    using var cmd = new MySqlCommand(sql, conn);
                    conn.Open();
                    using var reader = cmd.ExecuteReader();
                    Console.WriteLine("Cuisiniers avec plus de livraisons que la moyenne :");
                    while (reader.Read())
                    {
                        Console.WriteLine($"Cuisinier {reader.GetString(0)} - {reader.GetInt32(1)} livraisons");
                    }
                }

                // 2. Clients sans aucune commande
                public void AfficherClientsSansCommandes()
                {
                    string sql = @"
SELECT c.Id, c.nom, c.prénom
FROM Compte c
JOIN Client cl ON cl.Id = c.Id
WHERE NOT EXISTS (
    SELECT 1
    FROM Commande cmd
    WHERE cmd.idClient = cl.idClient
)";
                    using var conn = new MySqlConnection(connectionString);
                    using var cmd = new MySqlCommand(sql, conn);
                    conn.Open();
                    using var reader = cmd.ExecuteReader();
                    Console.WriteLine("Clients n'ayant jamais passé de commande :");
                    while (reader.Read())
                    {
                        Console.WriteLine($"Client {reader.GetString(1)} {reader.GetString(2)} (ID Compte: {reader.GetString(0)})");
                    }
                }

                // 3. Plats les plus chers de leur pays (ALL)
                public void AfficherPlatsLesPlusChersParPays()
                {
                    string sql = @"
SELECT p1.idPlat, p1.Recette, p1.Prix, p1.PaysOrigine
FROM Plat p1
WHERE p1.Prix > ALL (
    SELECT p2.Prix
    FROM Plat p2
    WHERE p2.PaysOrigine = p1.PaysOrigine
      AND p2.idPlat <> p1.idPlat
)";
                    using var conn = new MySqlConnection(connectionString);
                    using var cmd = new MySqlCommand(sql, conn);
                    conn.Open();
                    using var reader = cmd.ExecuteReader();
                    Console.WriteLine("Plats les plus chers par pays :");
                    while (reader.Read())
                    {
                        Console.WriteLine($"Plat: {reader.GetString(1)} - {reader.GetDouble(2)}€ ({reader.GetString(3)})");
                    }
                }

                // 4. Clients avec au moins une commande > 100€
                public void AfficherClientsAvecCommandesHautPrix()
                {
                    string sql = @"
SELECT DISTINCT cl.idClient, c.nom, c.prénom
FROM Commande cmd
JOIN Client cl ON cl.idClient = cmd.idClient
JOIN Compte c ON cl.Id = c.Id
WHERE cmd.CoutTotal > ANY (SELECT 100)";
                    using var conn = new MySqlConnection(connectionString);
                    using var cmd = new MySqlCommand(sql, conn);
                    conn.Open();
                    using var reader = cmd.ExecuteReader();
                    Console.WriteLine("Clients ayant commandé au moins une fois pour plus de 100€ :");
                    while (reader.Read())
                    {
                        Console.WriteLine($"Client: {reader.GetString(1)} {reader.GetString(2)} - ID: {reader.GetString(0)}");
                    }
                }

                // 5. Plats jamais commandés
                public void AfficherPlatsJamaisCommandés()
                {
                    string sql = @"
SELECT p.idPlat, p.Recette
FROM Plat p
LEFT JOIN LigneDeCommande ldc ON p.idPlat = ldc.idPlat
WHERE ldc.idPlat IS NULL";
                    using var conn = new MySqlConnection(connectionString);
                    using var cmd = new MySqlCommand(sql, conn);
                    conn.Open();
                    using var reader = cmd.ExecuteReader();
                    Console.WriteLine("Plats jamais commandés :");
                    while (reader.Read())
                    {
                        Console.WriteLine($"Plat: {reader.GetString(1)} - ID: {reader.GetString(0)}");
                    }
                }
            }


            /// <summary>
            /// génére le graphe
            /// </summary>
            /// <param name="chemin"></param>
            /// <param name="nomsGares"></param>
            public void GenererImageGraphe(string chemin, List<string>? nomsGares = null)
            {
                const int W = 4500, H = 4500;
                Random rnd = new();

                // Palette de 10 couleurs, extensible
                List<SKColor> palette = new()
        {
            SKColors.Blue, SKColors.Red,   SKColors.Green,  SKColors.Orange,
            SKColors.Purple, SKColors.Brown, SKColors.Cyan, SKColors.Magenta,
            SKColors.Yellow, SKColors.Lime
        };

                var couleurParNoeud = AssocierCouleurs();

                using var surface = SKSurface.Create(new SKImageInfo(W, H));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                SKPaint edgePaint = new() { Color = SKColors.Black, StrokeWidth = 2 };
                SKPaint nodePaint = new() { StrokeWidth = 2 };
                SKPaint textPaint = new() { Color = SKColors.Black, TextSize = 36, IsAntialias = true };
                SKPaint weightPaint = new() { Color = SKColors.Red, TextSize = 28, IsAntialias = true };

                if (L_Adjacence.Count == 0) return;

                // Placement : barycentre = nœud de plus fort degré
                Noeud centre = L_Adjacence.OrderByDescending(n => n.Connexion.Count).First();
                var periph = L_Adjacence.Where(n => n != centre).ToList();

                const float rBase = 40;
                const float minDist = 100;
                Dictionary<int, SKPoint> pos = new() { [centre.Numéro] = new SKPoint(W / 2f, H / 2f) };

                bool Overlap(SKPoint p)
                {
                    foreach (var q in pos.Values)
                        if (((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y)) < (rBase + minDist) * (rBase + minDist))
                            return true;
                    return false;
                }

                foreach (var n in periph)
                {
                    SKPoint p;
                    do { p = new SKPoint((float)(rnd.NextDouble() * W), (float)(rnd.NextDouble() * H)); }
                    while (Overlap(p));

                    pos[n.Numéro] = p;
                }

                // Arêtes + poids
                foreach (var a in L_Adjacence)
                    foreach (var b in a.Connexion.Where(v => a.Numéro < v))
                    {
                        canvas.DrawLine(pos[a.Numéro], pos[b], edgePaint);

                        float midX = (pos[a.Numéro].X + pos[b].X) / 2;
                        float midY = (pos[a.Numéro].Y + pos[b].Y) / 2;
                        canvas.DrawText(M_Adjacence[a.Numéro, b].ToString(), midX, midY, weightPaint);
                    }

                // Sommets + libellés
                foreach (var n in L_Adjacence)
                {
                    float size = Math.Max(rBase, 10 + 4 * n.Connexion.Count);
                    SKPoint p = pos[n.Numéro];

                    int idx = couleurParNoeud[n.Numéro];
                    nodePaint.Color = idx < palette.Count
                                      ? palette[idx]
                                      : SKColor.FromHsv((idx * 37) % 360, 80, 90); // teinte HSV si palette dépassée

                    canvas.DrawCircle(p, size, nodePaint);

                    string libelle = (nomsGares != null && n.Numéro <= nomsGares.Count && nomsGares[n.Numéro - 1] != null)
                                     ? nomsGares[n.Numéro - 1]
                                     : n.Numéro.ToString();

                    canvas.DrawText(libelle,
                                    p.X - libelle.Length * 12 / 2f,
                                    p.Y + 12,
                                    textPaint);
                }

                using var img = surface.Snapshot();
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.OpenWrite(chemin);
                data.SaveTo(file);
            }


            public class Client
            {
                public string IdClient { get; set; }       
                public string IdCompte { get; set; }       
                public string Nom { get; set; }
                public string Prenom { get; set; }
                public int Telephone { get; set; }
                public string AdresseMail { get; set; }
                public int Numero { get; set; }
                public string Rue { get; set; }
                public int CodePostal { get; set; }
                public string Ville { get; set; }
                public string MetroLePlusProche { get; set; }
                public bool Rade { get; set; }
                public string Type { get; set; }           
                public decimal AchatsCumules { get; set; }   
            }

            
           
            
            public class ClientRepository
            {
                private readonly string connectionString;

                public ClientRepository(string connectionString)
                {
                    this.connectionString = connectionString;
                }

                public void Export(string path, bool xml = false)
                {
                    var data = ObtenirClients("nom");             // ou autre méthode

                    if (xml) XmlHelper.Save(data, path);
                    else JsonHelper.Save(data, path);

                    // ✅ message visuel
                    Console.WriteLine($"[OK] Fichier exporté : {Path.GetFullPath(path)}");
                }

                public void Import(string path, bool xml = false)
                {
                    // 1. on lit le fichier
                    var data = xml
                        ? XmlHelper.Load<Client>(path)      // retourne List<Client>
                        : JsonHelper.Load<Client>(path);

                    // 2. pour chaque client du fichier …
                    foreach (var c in data)
                    {
                        bool existe = ObtenirClients("nom") // on regarde s’il est déjà en BD
                                       .Any(x => x.IdClient == c.IdClient);

                        if (existe) ModifierClient(c);     // -> mise à jour
                        else AjouterClient(c);      // -> insertion
                    }
                }

                public void AjouterClient(Client client)
                {
                    string sqlCompte = @"
INSERT INTO Compte (Id, nom, prénom, téléphone, adresse_mail, numéro, rue, Code_Postal, Ville, MetroLePlusProche, Radié)
VALUES (@Id, @Nom, @Prenom, @Telephone, @AdresseMail, @Numero, @Rue, @CodePostal, @Ville, @MetroLePlusProche, @Rade)
";
                    string sqlClient = @"
INSERT INTO Client (idClient, Type, Id)
VALUES (@idClient, @Type, @Id)
";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                                
                                using (MySqlCommand cmd = new MySqlCommand(sqlCompte, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Id", client.IdCompte);
                                    cmd.Parameters.AddWithValue("@Nom", client.Nom);
                                    cmd.Parameters.AddWithValue("@Prenom", client.Prenom);
                                    cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
                                    cmd.Parameters.AddWithValue("@AdresseMail", client.AdresseMail);
                                    cmd.Parameters.AddWithValue("@Numero", client.Numero);
                                    cmd.Parameters.AddWithValue("@Rue", client.Rue);
                                    cmd.Parameters.AddWithValue("@CodePostal", client.CodePostal);
                                    cmd.Parameters.AddWithValue("@Ville", client.Ville);
                                    cmd.Parameters.AddWithValue("@MetroLePlusProche", client.MetroLePlusProche);
                                    cmd.Parameters.AddWithValue("@Rade", client.Rade);
                                    cmd.ExecuteNonQuery();
                                }

                                
                                using (MySqlCommand cmd = new MySqlCommand(sqlClient, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@idClient", client.IdClient);
                                    cmd.Parameters.AddWithValue("@Type", client.Type);
                                    cmd.Parameters.AddWithValue("@Id", client.IdCompte);
                                    cmd.ExecuteNonQuery();
                                }

                                tran.Commit();
                                Console.WriteLine("Client ajouté avec succès.");
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                Console.WriteLine("Erreur lors de l'ajout du client : " + ex.Message);
                            }
                        }
                    }
                }

                
                public void SupprimerClient(string idClient)
                {
                   
                    string sqlSelectId = "SELECT Id FROM Client WHERE idClient = @idClient";
                    string sqlDeleteClient = "DELETE FROM Client WHERE idClient = @idClient";
                    string sqlDeleteCompte = "DELETE FROM Compte WHERE Id = @Id";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                                string idCompte = null;
                                using (MySqlCommand cmd = new MySqlCommand(sqlSelectId, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@idClient", idClient);
                                    object result = cmd.ExecuteScalar();
                                    if (result != null)
                                    {
                                        idCompte = result.ToString();
                                    }
                                }

                                if (string.IsNullOrEmpty(idCompte))
                                {
                                    Console.WriteLine("Client non trouvé.");
                                    return;
                                }

                                using (MySqlCommand cmd = new         MySqlCommand(sqlDeleteClient, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@idClient", idClient);
                                    cmd.ExecuteNonQuery();
                                }

                                using (MySqlCommand cmd = new MySqlCommand(sqlDeleteCompte, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Id", idCompte);
                                    cmd.ExecuteNonQuery();
                                }

                                tran.Commit();
                                Console.WriteLine("Client supprimé avec succès.");
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                Console.WriteLine("Erreur lors de la suppression du client : " + ex.Message);
                            }
                        }
                    }
                }

                
                public void ModifierClient(Client client)
                {
                    string sqlUpdateCompte = @"
UPDATE Compte
SET nom = @Nom,
    prénom = @Prenom,
    téléphone = @Telephone,
    adresse_mail = @AdresseMail,
    numéro = @Numero,
    rue = @Rue,
    Code_Postal = @CodePostal,
    Ville = @Ville,
    MetroLePlusProche = @MetroLePlusProche,
    Radié = @Rade
WHERE Id = @Id
";
                    string sqlUpdateClient = @"
UPDATE Client
SET Type = @Type
WHERE idClient = @idClient
";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                                using (MySqlCommand cmd = new MySqlCommand(sqlUpdateCompte, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Nom", client.Nom);
                                    cmd.Parameters.AddWithValue("@Prenom", client.Prenom);
                                    cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
                                    cmd.Parameters.AddWithValue("@AdresseMail", client.AdresseMail);
                                    cmd.Parameters.AddWithValue("@Numero", client.Numero);
                                    cmd.Parameters.AddWithValue("@Rue", client.Rue);
                                    cmd.Parameters.AddWithValue("@CodePostal", client.CodePostal);
                                    cmd.Parameters.AddWithValue("@Ville", client.Ville);
                                    cmd.Parameters.AddWithValue("@MetroLePlusProche", client.MetroLePlusProche);
                                    cmd.Parameters.AddWithValue("@Rade", client.Rade);
                                    cmd.Parameters.AddWithValue("@Id", client.IdCompte);
                                    cmd.ExecuteNonQuery();
                                }

                                using (MySqlCommand cmd = new MySqlCommand(sqlUpdateClient, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Type", client.Type);
                                    cmd.Parameters.AddWithValue("@idClient", client.IdClient);
                                    cmd.ExecuteNonQuery();
                                }
                                tran.Commit();
                                Console.WriteLine("Client modifié avec succès.");
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                Console.WriteLine("Erreur lors de la modification du client : " + ex.Message);
                            }
                        }
                    }
                }

                
                public List<Client> ObtenirClients(string critereTri)
                {
                    string orderBy;
                    if (critereTri == "nom")
                        orderBy = "c.nom ASC";
                    else if (critereTri == "rue")
                        orderBy = "c.rue ASC";
                    else if (critereTri == "achats")
                        orderBy = "IFNULL(SUM(co.CoutTotal), 0) DESC";
                    else
                        orderBy = "c.nom ASC";

                    string sql = $@"
SELECT cl.idClient, c.Id as IdCompte, c.nom, c.prénom, c.téléphone, c.adresse_mail, c.numéro, c.rue, c.Code_Postal, c.Ville, c.MetroLePlusProche, c.Radié, cl.Type,
       IFNULL(SUM(co.CoutTotal), 0) as AchatsCumules
FROM Client cl
JOIN Compte c ON cl.Id = c.Id
LEFT JOIN Commande co ON cl.idClient = co.idClient
GROUP BY cl.idClient, c.Id, c.nom, c.prénom, c.téléphone, c.adresse_mail, c.numéro, c.rue, c.Code_Postal, c.Ville, c.MetroLePlusProche, c.Radié, cl.Type
ORDER BY {orderBy}
";
                    List<Client> liste = new List<Client>();
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        conn.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                liste.Add(new Client
                                {
                                    IdClient = reader.GetString(0),
                                    IdCompte = reader.GetString(1),
                                    Nom = reader.GetString(2),
                                    Prenom = reader.GetString(3),
                                    Telephone = reader.GetInt32(4),
                                    AdresseMail = reader.GetString(5),
                                    Numero = reader.GetInt32(6),
                                    Rue = reader.GetString(7),
                                    CodePostal = reader.GetInt32(8),
                                    Ville = reader.GetString(9),
                                    MetroLePlusProche = reader.GetString(10),
                                    Rade = reader.GetBoolean(11),
                                    Type = reader.GetString(12),
                                    AchatsCumules = reader.GetDecimal(13)
                                });
                            }
                        }
                    }
                    return liste;
                }
            }
            public class Cuisinier
            {
                public string IdCuisinier { get; set; }      
                public string IdCompte { get; set; }          
                public string Nom { get; set; }
                public string Prenom { get; set; }
                public int Telephone { get; set; }
                public string AdresseMail { get; set; }
                public int Numero { get; set; }
                public string Rue { get; set; }
                public int CodePostal { get; set; }
                public string Ville { get; set; }
                public string MetroLePlusProche { get; set; }
                public bool Radié { get; set; }
                public string Type { get; set; }            
            }

            public class CuisinierRepository
            {
                private readonly string connectionString;

                public CuisinierRepository(string connectionString)
                {
                    this.connectionString = connectionString;
                }

               
                public void AjouterCuisinier(Cuisinier c)
                {
                    string sqlCompte = @"
INSERT INTO Compte (Id, nom, prénom, téléphone, adresse_mail, numéro, rue, Code_Postal, Ville, MetroLePlusProche, Radié)
VALUES (@Id, @Nom, @Prenom, @Telephone, @AdresseMail, @Numero, @Rue, @CodePostal, @Ville, @MetroLePlusProche, @Radié)
";
                    string sqlCuisinier = @"
INSERT INTO Cuisinier (idCuisinier, Type, Id)
VALUES (@idCuisinier, @Type, @Id)
";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                               
                                using (MySqlCommand cmd = new MySqlCommand(sqlCompte, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Id", c.IdCompte);
                                    cmd.Parameters.AddWithValue("@Nom", c.Nom);
                                    cmd.Parameters.AddWithValue("@Prenom", c.Prenom);
                                    cmd.Parameters.AddWithValue("@Telephone", c.Telephone);
                                    cmd.Parameters.AddWithValue("@AdresseMail", c.AdresseMail);
                                    cmd.Parameters.AddWithValue("@Numero", c.Numero);
                                    cmd.Parameters.AddWithValue("@Rue", c.Rue);
                                    cmd.Parameters.AddWithValue("@CodePostal", c.CodePostal);
                                    cmd.Parameters.AddWithValue("@Ville", c.Ville);
                                    cmd.Parameters.AddWithValue("@MetroLePlusProche", c.MetroLePlusProche);
                                    cmd.Parameters.AddWithValue("@Radié", c.Radié);
                                    cmd.ExecuteNonQuery();
                                }

                                
                                using (MySqlCommand cmd = new MySqlCommand(sqlCuisinier, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@idCuisinier", c.IdCuisinier);
                                    cmd.Parameters.AddWithValue("@Type", c.Type);
                                    cmd.Parameters.AddWithValue("@Id", c.IdCompte);
                                    cmd.ExecuteNonQuery();
                                }

                                tran.Commit();
                                Console.WriteLine("Cuisinier ajouté avec succès.");
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                Console.WriteLine("Erreur lors de l'ajout du cuisinier : " + ex.Message);
                            }
                        }
                    }
                }

                
                public void ModifierCuisinier(Cuisinier c)
                {
                    string sqlUpdateCompte = @"
UPDATE Compte
SET nom = @Nom,
    prénom = @Prenom,
    téléphone = @Telephone,
    adresse_mail = @AdresseMail,
    numéro = @Numero,
    rue = @Rue,
    Code_Postal = @CodePostal,
    Ville = @Ville,
    MetroLePlusProche = @MetroLePlusProche,
    Radié = @Radié
WHERE Id = @Id
";
                    string sqlUpdateCuisinier = @"
UPDATE Cuisinier
SET Type = @Type
WHERE idCuisinier = @idCuisinier
";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                                using (MySqlCommand cmd = new MySqlCommand(sqlUpdateCompte, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Nom", c.Nom);
                                    cmd.Parameters.AddWithValue("@Prenom", c.Prenom);
                                    cmd.Parameters.AddWithValue("@Telephone", c.Telephone);
                                    cmd.Parameters.AddWithValue("@AdresseMail", c.AdresseMail);
                                    cmd.Parameters.AddWithValue("@Numero", c.Numero);
                                    cmd.Parameters.AddWithValue("@Rue", c.Rue);
                                    cmd.Parameters.AddWithValue("@CodePostal", c.CodePostal);
                                    cmd.Parameters.AddWithValue("@Ville", c.Ville);
                                    cmd.Parameters.AddWithValue("@MetroLePlusProche", c.MetroLePlusProche);
                                    cmd.Parameters.AddWithValue("@Radié", c.Radié);
                                    cmd.Parameters.AddWithValue("@Id", c.IdCompte);
                                    cmd.ExecuteNonQuery();
                                }
                                using (MySqlCommand cmd = new MySqlCommand(sqlUpdateCuisinier, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Type", c.Type);
                                    cmd.Parameters.AddWithValue("@idCuisinier", c.IdCuisinier);
                                    cmd.ExecuteNonQuery();
                                }
                                tran.Commit();
                                Console.WriteLine("Cuisinier modifié avec succès.");
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                Console.WriteLine("Erreur lors de la modification du cuisinier : " + ex.Message);
                            }
                        }
                    }
                }

                
                public void SupprimerCuisinier(string idCuisinier)
                {
                    string sqlSelectId = "SELECT Id FROM Cuisinier WHERE idCuisinier = @idCuisinier";
                    string sqlDeleteCuisinier = "DELETE FROM Cuisinier WHERE idCuisinier = @idCuisinier";
                    string sqlDeleteCompte = "DELETE FROM Compte WHERE Id = @Id";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                                string idCompte = null;
                                using (MySqlCommand cmd = new MySqlCommand(sqlSelectId, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@idCuisinier", idCuisinier);
                                    object result = cmd.ExecuteScalar();
                                    if (result != null)
                                        idCompte = result.ToString();
                                }
                                if (string.IsNullOrEmpty(idCompte))
                                {
                                    Console.WriteLine("Cuisinier non trouvé.");
                                    return;
                                }
                                using (MySqlCommand cmd = new MySqlCommand(sqlDeleteCuisinier, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@idCuisinier", idCuisinier);
                                    cmd.ExecuteNonQuery();
                                }
                                using (MySqlCommand cmd = new MySqlCommand(sqlDeleteCompte, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Id", idCompte);
                                    cmd.ExecuteNonQuery();
                                }
                                tran.Commit();
                                Console.WriteLine("Cuisinier supprimé avec succès.");
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                Console.WriteLine("Erreur lors de la suppression du cuisinier : " + ex.Message);
                            }
                        }
                    }
                }

               
                public void AfficherClientsServis(string idCuisinier, DateTime? dateDebut = null, DateTime? dateFin = null)
                {
                    string sql = @"
SELECT DISTINCT co.Id, co.nom, co.prénom, cmd.Date_Commande
FROM Cuisinier cu
JOIN Effectue e ON cu.idCuisinier = e.idCuisinier
JOIN Livraison l ON e.idLivraison = l.idLivraison
JOIN LigneDeCommande ldc ON l.idLigneDeCommande = ldc.idLigneDeCommande
JOIN Commande cmd ON ldc.idCommande = cmd.idCommande
JOIN Client cl ON cmd.idClient = cl.idClient
JOIN Compte co ON cl.Id = co.Id
WHERE cu.idCuisinier = @idCuisinier
";
                    if (dateDebut.HasValue)
                    {
                        sql += " AND cmd.Date_Commande >= @dateDebut";
                    }
                    if (dateFin.HasValue)
                    {
                        sql += " AND cmd.Date_Commande <= @dateFin";
                    }
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idCuisinier", idCuisinier);
                        if (dateDebut.HasValue)
                            cmd.Parameters.AddWithValue("@dateDebut", dateDebut.Value);
                        if (dateFin.HasValue)
                            cmd.Parameters.AddWithValue("@dateFin", dateFin.Value);
                        conn.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Clients servis par le cuisinier :");
                            while (reader.Read())
                            {
                                string clientId = reader.GetString(0);
                                string nom = reader.GetString(1);
                                string prenom = reader.GetString(2);
                                DateTime dateCommande = reader.GetDateTime(3);
                                Console.WriteLine($"Client: {nom} {prenom} (ID: {clientId}) - Commande le: {dateCommande.ToShortDateString()}");
                            }
                        }
                    }
                }

                
                public void AfficherPlatsParFrequence(string idCuisinier)
                {
                    string sql = @"
SELECT p.idPlat, p.Recette, COUNT(*) AS Frequency
FROM Cuisinier cu
JOIN Effectue e ON cu.idCuisinier = e.idCuisinier
JOIN Livraison l ON e.idLivraison = l.idLivraison
JOIN LigneDeCommande ldc ON l.idLigneDeCommande = ldc.idLigneDeCommande
JOIN Plat p ON ldc.idPlat = p.idPlat
WHERE cu.idCuisinier = @idCuisinier
GROUP BY p.idPlat, p.Recette
ORDER BY Frequency DESC
";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idCuisinier", idCuisinier);
                        conn.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Plats réalisés par le cuisinier (par fréquence) :");
                            while (reader.Read())
                            {
                                string idPlat = reader.GetString(0);
                                string recette = reader.GetString(1);
                                int freq = reader.GetInt32(2);
                                Console.WriteLine($"Plat: {recette} (ID: {idPlat}) - Réalisé {freq} fois");
                            }
                        }
                    }
                }

               
                public void AfficherPlatDuJour(string idCuisinier)
                {
                    string sql = @"
SELECT TOP 1 p.idPlat, p.Recette, COUNT(*) AS Frequency
FROM Cuisinier cu
JOIN Effectue e ON cu.idCuisinier = e.idCuisinier
JOIN Livraison l ON e.idLivraison = l.idLivraison
JOIN LigneDeCommande ldc ON l.idLigneDeCommande = ldc.idLigneDeCommande
JOIN Commande cmd ON ldc.idCommande = cmd.idCommande
JOIN Plat p ON ldc.idPlat = p.idPlat
WHERE cu.idCuisinier = @idCuisinier
  AND cmd.Date_Commande = CONVERT(date, GETDATE())
GROUP BY p.idPlat, p.Recette
ORDER BY Frequency DESC
";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idCuisinier", idCuisinier);
                        conn.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Plat du jour proposé par le cuisinier :");
                            if (reader.Read())
                            {
                                string idPlat = reader.GetString(0);
                                string recette = reader.GetString(1);
                                int freq = reader.GetInt32(2);
                                Console.WriteLine($"Plat: {recette} (ID: {idPlat}) - Réalisé {freq} fois aujourd'hui");
                            }
                            else
                            {
                                Console.WriteLine("Aucun plat pour aujourd'hui.");
                            }
                        }
                    }
                }

            }
            public class Commande
            {
                public string IdCommande { get; set; }
                public DateTime DateCommande { get; set; }
                public int CoutTotal { get; set; }   
                public string IdClient { get; set; }
                public int Depart { get; set; }      
                public int Arrivee { get; set; }     
            }

            public class BilanRepository
            {
                private readonly string connectionString;
                public BilanRepository(string connectionString)
                {
                    this.connectionString = connectionString;
                }

                
                public void AfficherLivraisonsParCuisinier()
                {
                    string sql = @"
SELECT cu.idCuisinier, COUNT(DISTINCT l.idLivraison) AS NombreLivraisons
FROM Cuisinier cu
JOIN Effectue e ON cu.idCuisinier = e.idCuisinier
JOIN Livraison l ON e.idLivraison = l.idLivraison
GROUP BY cu.idCuisinier";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Livraisons par cuisinier :");
                            while (reader.Read())
                            {
                                string idCuisinier = reader.GetString(0);
                                int nbLivraisons = reader.GetInt32(1);
                                Console.WriteLine($"Cuisinier {idCuisinier} a effectué {nbLivraisons} livraisons.");
                            }
                        }
                    }
                }

                
                public List<Commande> ObtenirCommandesParPeriode(DateTime? debut, DateTime? fin)
                {
                    List<Commande> liste = new List<Commande>();
                    string sql = "SELECT idCommande, Date_Commande, CoutTotal, idClient FROM Commande WHERE 1=1";
                    if (debut.HasValue)
                        sql += " AND Date_Commande >= @debut";
                    if (fin.HasValue)
                        sql += " AND Date_Commande <= @fin";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            if (debut.HasValue)
                                cmd.Parameters.AddWithValue("@debut", debut.Value);
                            if (fin.HasValue)
                                cmd.Parameters.AddWithValue("@fin", fin.Value);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Commande commande = new Commande
                                    {
                                        IdCommande = reader.GetString(0),
                                        DateCommande = reader.GetDateTime(1),
                                        CoutTotal = reader.GetInt32(2),
                                        IdClient = reader.GetString(3)
                                    };
                                    liste.Add(commande);
                                }
                            }
                        }
                    }
                    return liste;
                }

                
                public double CalculerMoyennePrixCommandes()
                {
                    string sql = "SELECT AVG(CAST(CoutTotal AS FLOAT)) FROM Commande";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            object result = cmd.ExecuteScalar();
                            return (result != DBNull.Value) ? Convert.ToDouble(result) : 0;
                        }
                    }
                }

               
                public double CalculerMoyenneAchatsClients()
                {
                    string sql = @"
SELECT AVG(TotalAchats) FROM (
    SELECT IFNULL(SUM(CoutTotal), 0) AS TotalAchats
    FROM Commande
    GROUP BY idClient
) as Achats";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            object result = cmd.ExecuteScalar();
                            return (result != DBNull.Value) ? Convert.ToDouble(result) : 0;
                        }
                    }
                }

               
                public void AfficherCommandesPourClient(string idClient, string pays, DateTime? debut, DateTime? fin)
                {
                    string sql = @"
SELECT cmd.idCommande, cmd.Date_Commande, cmd.CoutTotal, p.PaysOrigine
FROM Commande cmd
JOIN LigneDeCommande ldc ON cmd.idCommande = ldc.idCommande
JOIN Plat p ON ldc.idPlat = p.idPlat
WHERE cmd.idClient = @idClient";
                    if (!string.IsNullOrEmpty(pays))
                        sql += " AND p.PaysOrigine = @pays";
                    if (debut.HasValue)
                        sql += " AND cmd.Date_Commande >= @debut";
                    if (fin.HasValue)
                        sql += " AND cmd.Date_Commande <= @fin";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@idClient", idClient);
                            if (!string.IsNullOrEmpty(pays))
                                cmd.Parameters.AddWithValue("@pays", pays);
                            if (debut.HasValue)
                                cmd.Parameters.AddWithValue("@debut", debut.Value);
                            if (fin.HasValue)
                                cmd.Parameters.AddWithValue("@fin", fin.Value);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                Console.WriteLine($"Commandes pour le client {idClient} (Pays des plats = {pays}, Période = {debut?.ToShortDateString()} - {fin?.ToShortDateString()}) :");
                                while (reader.Read())
                                {
                                    string idCmd = reader.GetString(0);
                                    DateTime dateCmd = reader.GetDateTime(1);
                                    int cout = reader.GetInt32(2);
                                    string paysOrigine = reader.GetString(3);
                                    Console.WriteLine($"Commande {idCmd}, Date: {dateCmd.ToShortDateString()}, Cout: {cout}, Plat de: {paysOrigine}");
                                }
                            }
                        }
                    }
                }
            }

          
            public class CommandeRepository
            {
                private readonly string connectionString;
                public CommandeRepository(string connectionString)
                {
                    this.connectionString = connectionString;
                }
                public void AjouterCommande(Commande cmd)
                {
                    string sql = @"
INSERT INTO Commande (idCommande, Date_Commande, CoutTotal, idClient)
VALUES (@idCommande, @DateCommande, @CoutTotal, @idClient)";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand command = new MySqlCommand(sql, conn))
                        {
                            command.Parameters.AddWithValue("@idCommande", cmd.IdCommande);
                            command.Parameters.AddWithValue("@DateCommande", cmd.DateCommande);
                            command.Parameters.AddWithValue("@CoutTotal", cmd.CoutTotal);
                            command.Parameters.AddWithValue("@idClient", cmd.IdClient);
                            command.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("Commande ajoutée avec succès.");
                }
                public void ModifierCommande(Commande cmd)
                {
                    string sql = @"
UPDATE Commande
SET Date_Commande = @DateCommande, CoutTotal = @CoutTotal, idClient = @idClient
WHERE idCommande = @idCommande";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand command = new MySqlCommand(sql, conn))
                        {
                            command.Parameters.AddWithValue("@idCommande", cmd.IdCommande);
                            command.Parameters.AddWithValue("@DateCommande", cmd.DateCommande);
                            command.Parameters.AddWithValue("@CoutTotal", cmd.CoutTotal);
                            command.Parameters.AddWithValue("@idClient", cmd.IdClient);
                            command.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("Commande modifiée avec succès.");
                }
                public Commande ObtenirCommande(string idCommande)
                {
                    string sql = "SELECT idCommande, Date_Commande, CoutTotal, idClient FROM Commande WHERE idCommande = @idCommande";
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand command = new MySqlCommand(sql, conn))
                        {
                            command.Parameters.AddWithValue("@idCommande", idCommande);
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return new Commande
                                    {
                                        IdCommande = reader.GetString(0),
                                        DateCommande = reader.GetDateTime(1),
                                        CoutTotal = reader.GetInt32(2),
                                        IdClient = reader.GetString(3)
                                    };
                                }
                            }
                        }
                    }
                    return null;
                }
            }



            /// <summary>
            /// Lis le ficchier et remplis les infos
            /// </summary>
            /// <param name="cheminFichier"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public static (List<string> nomsGares, List<Program.Lien> liens) ChargerDepuisExcel(string cheminFichier)
        {
            var correspondancesAjoutees = new HashSet<int>();
            var nomsGares = new List<string>();
            var liens = new List<Program.Lien>();
            var nomToId = new Dictionary<string, int>();
            var liensAjoutes = new HashSet<string>(); 

            using (var package = new ExcelPackage(new FileInfo(cheminFichier)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                    throw new Exception("Le fichier Excel ne contient aucune feuille.");

                var feuille = package.Workbook.Worksheets[1]; 
                int nbLignes = feuille.Dimension.Rows;

                for (int ligne = 2; ligne <= nbLignes; ligne++) 
                {
                    string nomGare =  feuille.Cells[ligne, 2].Text.Trim();     
                    string precedent  = feuille.Cells[ligne, 3].Text.Trim(); 
                    string suivant =  feuille.Cells[ligne, 4].Text.Trim();  
                    string tempsTxt =  feuille.Cells[ligne, 5].Text.Trim();  
                    string changement =  feuille.Cells[ligne, 6].Text.Trim(); 

                    if (string.IsNullOrWhiteSpace(nomGare)) continue;

                    if (!nomToId.ContainsKey(nomGare))
                    {
                        nomToId[nomGare] = nomToId.Count + 1;
                        nomsGares.Add(nomGare);
                    }
                    int idGare = nomToId[nomGare];

                    int.TryParse(tempsTxt, out int temps);
                    if (temps <= 0) temps = 1;

                    void AjouterLien(string idVoisine)
                    {
                        if (string.IsNullOrWhiteSpace(idVoisine)) return;

                        if (!int.TryParse(idVoisine, out int numVoisine)) return;

                        if (numVoisine < 1 || numVoisine + 1 > nbLignes) return;

                        string nomVoisine = feuille.Cells[numVoisine + 1, 2].Text.Trim(); 

                        if (string.IsNullOrWhiteSpace(nomVoisine)) return;

                        if (!nomToId.ContainsKey(nomVoisine))
                        {
                            nomToId[nomVoisine] = nomToId.Count + 1;
                            nomsGares.Add(nomVoisine);
                        }

                        int idDest = nomToId[nomVoisine];

                        
                        string cleLien = $"{Math.Min(idGare, idDest)}-{Math.Max(idGare, idDest)}";

                        if (!liensAjoutes.Contains(cleLien))
                        {
                            liens.Add(new Program.Lien { N1 = idGare, N2 = idDest, Poids = temps });
                            liensAjoutes.Add(cleLien);
                        }
                    }

                    AjouterLien(precedent);
                    AjouterLien(suivant);

                    
                    if (!string.IsNullOrWhiteSpace(changement) && int.TryParse(changement, out int tChgt) && tChgt > 0)
                    {
                        if (!correspondancesAjoutees.Contains(idGare))
                        {
                            liens.Add(new Program.Lien { N1 = idGare, N2 = idGare, Poids = tChgt });
                            correspondancesAjoutees.Add(idGare);
                        }

                    }

                }
            }

            return (nomsGares, liens);
        }




        static void Main(string[] args)
        {
            string cheminExcel = "MetroParis.xlsx";

            var (nomsGares, liens) = ChargerDepuisExcel(cheminExcel);

            Graphe graphe = new Graphe(nomsGares.Count, liens);

            Console.WriteLine("Index des gares :");
            for (int i = 0; i < nomsGares.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {nomsGares[i]}");
            }

            Console.WriteLine("\nListe d'adjacence :");
            graphe.Afficher_L_Adjacence();
            Console.WriteLine($"\nNombre de gares : {nomsGares.Count}");
            Console.WriteLine($"Nombre de liens : {liens.Count}");
            Console.WriteLine($"Nombre de noeuds dans le graphe : {graphe.L_Adjacence.Count}");
                var couleurs = graphe.NombreDeCouleurs();
                Console.WriteLine("Nombre couleurs= "+couleurs);

               
                Console.WriteLine("Le graphe est biparti : " + graphe.EstBiparti());

                
                Console.WriteLine("Le graphe est planaire : " + graphe.EstPlanaire());



                string imagePath = "reseau_gares.png";
            graphe.GenererImageGraphe(imagePath, nomsGares);
            Console.WriteLine($"\nImage générée : {imagePath}");

            Console.WriteLine("\nMatrice d'adjacence :");
            graphe.Afficher_M_Adjacence();

            graphe.Dijkstra("Châtelet", "Place de Clichy", nomsGares);
            graphe.BellmanFord("Châtelet", "Nation", nomsGares);
            graphe.FloydWarshall("Châtelet", "Place de Clichy", nomsGares);

                
                string connectionString =
               "Server=127.0.0.1;" +  
               "Port=3306;" +  
               "Database=PSI;" +
               "Uid=root;" +
               "Pwd=Gaabi.a3;";


                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connexion réussie !");
                    
                }
              






                ClientRepository repo = new ClientRepository(connectionString);

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n--- Gestion des Clients (SQL PSI) ---");
                Console.WriteLine("1. Ajouter un client");
                Console.WriteLine("2. Supprimer un client");
                Console.WriteLine("3. Modifier un client");
                Console.WriteLine("4. Afficher les clients");
                    Console.WriteLine("5. Exporter");
                    Console.WriteLine("6. Importer");
                    Console.WriteLine("0. Quitter");
                Console.Write("Votre choix : ");
                string choix = Console.ReadLine();
                switch (choix)
                {
                    case "1":
                        Client nouveauClient = new Client();
                        Console.Write("ID Compte (unique) : ");
                        nouveauClient.IdCompte = Console.ReadLine();
                        Console.Write("ID Client (unique) : ");
                        nouveauClient.IdClient = Console.ReadLine();
                        Console.Write("Nom : ");
                        nouveauClient.Nom = Console.ReadLine();
                        Console.Write("Prénom : ");
                        nouveauClient.Prenom = Console.ReadLine();
                        Console.Write("Téléphone : ");
                        int.TryParse(Console.ReadLine(), out int telephone);
                        nouveauClient.Telephone = telephone;
                        Console.Write("Adresse mail : ");
                        nouveauClient.AdresseMail = Console.ReadLine();
                        Console.Write("Numéro : ");
                        int.TryParse(Console.ReadLine(), out int numero);
                        nouveauClient.Numero = numero;
                        Console.Write("Rue : ");
                        nouveauClient.Rue = Console.ReadLine();
                        Console.Write("Code Postal : ");
                        int.TryParse(Console.ReadLine(), out int cp);
                        nouveauClient.CodePostal = cp;
                        Console.Write("Ville : ");
                        nouveauClient.Ville = Console.ReadLine();
                        Console.Write("Métro le plus proche : ");
                        nouveauClient.MetroLePlusProche = Console.ReadLine();
                        Console.Write("Radié (true/false) : ");
                        bool.TryParse(Console.ReadLine(), out bool radié);
                        nouveauClient.Rade = radié;
                        Console.Write("Type de client (Particulier ou Entreprise_locale) : ");
                        nouveauClient.Type = Console.ReadLine();

                        repo.AjouterClient(nouveauClient);
                        break;
                    case "2":
                        Console.Write("ID du client à supprimer : ");
                        string idClientSupp = Console.ReadLine();
                        repo.SupprimerClient(idClientSupp);
                        break;
                    case "3":
                        Client clientMod = new Client();
                        Console.Write("ID Client à modifier : ");
                        clientMod.IdClient = Console.ReadLine();
                        Console.Write("ID Compte associé : ");
                        clientMod.IdCompte = Console.ReadLine();
                        Console.Write("Nouveau nom : ");
                        clientMod.Nom = Console.ReadLine();
                        Console.Write("Nouveau prénom : ");
                        clientMod.Prenom = Console.ReadLine();
                        Console.Write("Nouveau téléphone : ");
                        int.TryParse(Console.ReadLine(), out int nouveauTel);
                        clientMod.Telephone = nouveauTel;
                        Console.Write("Nouvelle adresse mail : ");
                        clientMod.AdresseMail = Console.ReadLine();
                        Console.Write("Nouveau numéro : ");
                        int.TryParse(Console.ReadLine(), out int nouveauNum);
                        clientMod.Numero = nouveauNum;
                        Console.Write("Nouvelle rue : ");
                        clientMod.Rue = Console.ReadLine();
                        Console.Write("Nouveau Code Postal : ");
                        int.TryParse(Console.ReadLine(), out int nouveauCP);
                        clientMod.CodePostal = nouveauCP;
                        Console.Write("Nouvelle ville : ");
                        clientMod.Ville = Console.ReadLine();
                        Console.Write("Nouveau métro le plus proche : ");
                        clientMod.MetroLePlusProche = Console.ReadLine();
                        Console.Write("Radié (true/false) : ");
                        bool.TryParse(Console.ReadLine(), out bool nouveauRade);
                        clientMod.Rade = nouveauRade;
                        Console.Write("Nouveau type de client : ");
                        clientMod.Type = Console.ReadLine();

                        repo.ModifierClient(clientMod);
                        break;
                    case "4":
                        Console.WriteLine("Critères de tri disponibles : 'nom', 'rue', 'achats'");
                        Console.Write("Votre choix : ");
                        string critere = Console.ReadLine();
                        List<Client> clients = repo.ObtenirClients(critere);
                        Console.WriteLine("\nListe des Clients :");
                        foreach (var client in clients)
                        {
                            Console.WriteLine($"IDClient: {client.IdClient}, Nom: {client.Nom}, Prénom: {client.Prenom}, Rue: {client.Rue}");
                        }
                        break;
                    case "5":   // Exporter
                         repo.Export("Exports/clients.json");          // JSON
                            break;

                        case "6":  // Importer
                            repo.Import("Exports/clients_sample.json");          // JSON
                            break;
                        case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Option non reconnue.");
                        break;
                }
            }

                

                    CuisinierRepository cuisinierRepo = new CuisinierRepository(connectionString);
                bool exitCuisiniers = false;
                while (!exitCuisiniers)
                {
                    Console.WriteLine("\n--- Gestion des Cuisiniers ---");
                    Console.WriteLine("1. Ajouter un cuisinier");
                    Console.WriteLine("2. Modifier un cuisinier");
                    Console.WriteLine("3. Supprimer un cuisinier");
                    Console.WriteLine("4. Afficher les clients servis");
                    Console.WriteLine("5. Afficher les plats par fréquence");
                    Console.WriteLine("6. Afficher le plat du jour");
                    Console.WriteLine("0. Quitter");
                    Console.Write("Votre choix : ");
                    string choixCuisinier = Console.ReadLine();
                    switch (choixCuisinier)
                    {
                        case "1":
                            Cuisinier nouveauCuisinier = new Cuisinier();
                            Console.Write("ID Cuisinier (unique) : ");
                            nouveauCuisinier.IdCuisinier = Console.ReadLine();
                            Console.Write("ID Compte (unique) : ");
                            nouveauCuisinier.IdCompte = Console.ReadLine();
                            Console.Write("Nom : ");
                            nouveauCuisinier.Nom = Console.ReadLine();
                            Console.Write("Prénom : ");
                            nouveauCuisinier.Prenom = Console.ReadLine();
                            Console.Write("Téléphone : ");
                            int.TryParse(Console.ReadLine(), out int tel);
                            nouveauCuisinier.Telephone = tel;
                            Console.Write("Adresse mail : ");
                            nouveauCuisinier.AdresseMail = Console.ReadLine();
                            Console.Write("Numéro : ");
                            int.TryParse(Console.ReadLine(), out int num);
                            nouveauCuisinier.Numero = num;
                            Console.Write("Rue : ");
                            nouveauCuisinier.Rue = Console.ReadLine();
                            Console.Write("Code Postal : ");
                            int.TryParse(Console.ReadLine(), out int cp);
                            nouveauCuisinier.CodePostal = cp;
                            Console.Write("Ville : ");
                            nouveauCuisinier.Ville = Console.ReadLine();
                            Console.Write("Métro le plus proche : ");
                            nouveauCuisinier.MetroLePlusProche = Console.ReadLine();
                            Console.Write("Radié (true/false) : ");
                            bool.TryParse(Console.ReadLine(), out bool rad);
                            nouveauCuisinier.Radié = rad;
                            Console.Write("Type (ex: Cuisinier) : ");
                            nouveauCuisinier.Type = Console.ReadLine();

                            cuisinierRepo.AjouterCuisinier(nouveauCuisinier);
                            break;
                        case "2":
                            Cuisinier modifCuisinier = new Cuisinier();
                            Console.Write("ID Cuisinier à modifier : ");
                            modifCuisinier.IdCuisinier = Console.ReadLine();
                            Console.Write("ID Compte associé : ");
                            modifCuisinier.IdCompte = Console.ReadLine();
                            Console.Write("Nouveau nom : ");
                            modifCuisinier.Nom = Console.ReadLine();
                            Console.Write("Nouveau prénom : ");
                            modifCuisinier.Prenom = Console.ReadLine();
                            Console.Write("Nouveau téléphone : ");
                            int.TryParse(Console.ReadLine(), out int telMod);
                            modifCuisinier.Telephone = telMod;
                            Console.Write("Nouvelle adresse mail : ");
                            modifCuisinier.AdresseMail = Console.ReadLine();
                            Console.Write("Nouveau numéro : ");
                            int.TryParse(Console.ReadLine(), out int numMod);
                            modifCuisinier.Numero = numMod;
                            Console.Write("Nouvelle rue : ");
                            modifCuisinier.Rue = Console.ReadLine();
                            Console.Write("Nouveau Code Postal : ");
                            int.TryParse(Console.ReadLine(), out int cpMod);
                            modifCuisinier.CodePostal = cpMod;
                            Console.Write("Nouvelle ville : ");
                            modifCuisinier.Ville = Console.ReadLine();
                            Console.Write("Nouveau métro le plus proche : ");
                            modifCuisinier.MetroLePlusProche = Console.ReadLine();
                            Console.Write("Radié (true/false) : ");
                            bool.TryParse(Console.ReadLine(), out bool radMod);
                            modifCuisinier.Radié = radMod;
                            Console.Write("Nouveau type : ");
                            modifCuisinier.Type = Console.ReadLine();

                            cuisinierRepo.ModifierCuisinier(modifCuisinier);
                            break;
                        case "3":
                            Console.Write("ID Cuisinier à supprimer : ");
                            string idSuppr = Console.ReadLine();
                            cuisinierRepo.SupprimerCuisinier(idSuppr);
                            break;
                        case "4":
                            Console.Write("ID Cuisinier : ");
                            string idAffClients = Console.ReadLine();
                            Console.Write("Date de début (format AAAA-MM-JJ) ou appuyez sur Entrée pour tout afficher : ");
                            string dateDebutStr = Console.ReadLine();
                            DateTime? dateDebut = null;
                            if (!string.IsNullOrEmpty(dateDebutStr) && DateTime.TryParse(dateDebutStr, out DateTime dtDeb))
                            {
                                dateDebut = dtDeb;
                            }
                            Console.Write("Date de fin (format AAAA-MM-JJ) ou appuyez sur Entrée : ");
                            string dateFinStr = Console.ReadLine();
                            DateTime? dateFin = null;
                            if (!string.IsNullOrEmpty(dateFinStr) && DateTime.TryParse(dateFinStr, out DateTime dtFin))
                            {
                                dateFin = dtFin;
                            }
                            cuisinierRepo.AfficherClientsServis(idAffClients, dateDebut, dateFin);
                            break;
                        case "5":
                            Console.Write("ID Cuisinier : ");
                            string idAffPlats = Console.ReadLine();
                            cuisinierRepo.AfficherPlatsParFrequence(idAffPlats);
                            break;
                        case "6":
                            Console.Write("ID Cuisinier : ");
                            string idPlatJour = Console.ReadLine();
                            cuisinierRepo.AfficherPlatDuJour(idPlatJour);
                            break;
                        case "0":
                            exitCuisiniers = true;
                            break;
                        default:
                            Console.WriteLine("Option non reconnue.");
                            break;
                    }
                }

                CommandeRepository commandeRepo = new CommandeRepository(connectionString);
                bool exitCmd = false;
                while (!exitCmd)
                {
                    Console.WriteLine("\n--- Gestion des Commandes ---");
                    Console.WriteLine("1. Créer une nouvelle commande");
                    Console.WriteLine("2. Modifier une commande");
                    Console.WriteLine("3. Afficher le prix d'une commande");
                    Console.WriteLine("4. Afficher le trajet de livraison pour une commande");
                    Console.WriteLine("0. Quitter");
                    Console.Write("Votre choix : ");
                    string choixCmd = Console.ReadLine();
                    switch (choixCmd)
                    {
                        case "1":
                            
                            Commande nouvelleCmd = new Commande();
                            Console.Write("Numéro de commande (unique) : ");
                            nouvelleCmd.IdCommande = Console.ReadLine();
                            Console.Write("Date de commande (AAAA-MM-JJ) : ");
                            DateTime dateCmd;
                            while (!DateTime.TryParse(Console.ReadLine(), out dateCmd))
                            {
                                Console.Write("Format invalide. Réessayez (AAAA-MM-JJ) : ");
                            }
                            nouvelleCmd.DateCommande = dateCmd;
                            Console.Write("ID du client (si inexistant, créez-le via la gestion des clients) : ");
                            nouvelleCmd.IdClient = Console.ReadLine();
                            Console.Write("Indice de la gare de départ (entre 1 et {0}) : ", nomsGares.Count);
                            int dep;
                            while (!int.TryParse(Console.ReadLine(), out dep) || dep < 1 || dep > nomsGares.Count)
                            {
                                Console.Write("Valeur invalide. Réessayez : ");
                            }
                            nouvelleCmd.Depart = dep;
                            Console.Write("Indice de la gare d'arrivée (entre 1 et {0}) : ", nomsGares.Count);
                            int arr;
                            while (!int.TryParse(Console.ReadLine(), out arr) || arr < 1 || arr > nomsGares.Count)
                            {
                                Console.Write("Valeur invalide. Réessayez : ");
                            }
                            nouvelleCmd.Arrivee = arr;
                            
                            Console.WriteLine("Chemin de livraison calculé (via Dijkstra) :");
                            graphe.Dijkstra(nomsGares[dep - 1], nomsGares[arr - 1], nomsGares);
                            
                            Console.Write("Indiquez le coût (tel qu'affiché par Dijkstra) : ");
                            int cout;
                            while (!int.TryParse(Console.ReadLine(), out cout))
                            {
                                Console.Write("Valeur invalide. Réessayez : ");
                            }
                            nouvelleCmd.CoutTotal = cout;
                            commandeRepo.AjouterCommande(nouvelleCmd);
                            break;
                        case "2":
                            
                            Console.Write("Numéro de commande à modifier : ");
                            string idModif = Console.ReadLine();
                            Commande cmdModif = commandeRepo.ObtenirCommande(idModif);
                            if (cmdModif == null)
                            {
                                Console.WriteLine("Commande non trouvée.");
                                break;
                            }
                            Console.Write("Nouvelle date de commande (AAAA-MM-JJ) : ");
                            DateTime newDate;
                            while (!DateTime.TryParse(Console.ReadLine(), out newDate))
                            {
                                Console.Write("Format invalide. Réessayez : ");
                            }
                            cmdModif.DateCommande = newDate;
                            Console.Write("Nouvel indice de gare de départ : ");
                            int newDep;
                            while (!int.TryParse(Console.ReadLine(), out newDep) || newDep < 1 || newDep > nomsGares.Count)
                            {
                                Console.Write("Valeur invalide. Réessayez : ");
                            }
                            cmdModif.Depart = newDep;
                            Console.Write("Nouvel indice de gare d'arrivée : ");
                            int newArr;
                            while (!int.TryParse(Console.ReadLine(), out newArr) || newArr < 1 || newArr > nomsGares.Count)
                            {
                                Console.Write("Valeur invalide. Réessayez : ");
                            }
                            cmdModif.Arrivee = newArr;
                            Console.WriteLine("Nouveau chemin calculé (via Dijkstra) :");
                            graphe.Dijkstra(nomsGares[newDep - 1], nomsGares[newArr - 1], nomsGares);
                            Console.Write("Indiquez le nouveau coût (tel qu'affiché par Dijkstra) : ");
                            int newCout;
                            while (!int.TryParse(Console.ReadLine(), out newCout))
                            {
                                Console.Write("Valeur invalide. Réessayez : ");
                            }
                            cmdModif.CoutTotal = newCout;
                            commandeRepo.ModifierCommande(cmdModif);
                            break;
                        case "3":
                            
                            Console.Write("Numéro de commande : ");
                            string idPrix = Console.ReadLine();
                            Commande cmdPrix = commandeRepo.ObtenirCommande(idPrix);
                            if (cmdPrix == null)
                                Console.WriteLine("Commande non trouvée.");
                            else
                                Console.WriteLine($"Le prix de la commande {cmdPrix.IdCommande} est de {cmdPrix.CoutTotal}");
                            break;
                        case "4":
                            
                            Console.Write("Numéro de commande : ");
                            string idChemin = Console.ReadLine();
                            Commande cmdChemin = commandeRepo.ObtenirCommande(idChemin);
                            if (cmdChemin == null)
                            {
                                Console.WriteLine("Commande non trouvée.");
                                break;
                            }
                            Console.WriteLine($"Chemin pour la commande {cmdChemin.IdCommande} (de {nomsGares[cmdChemin.Depart - 1]} à {nomsGares[cmdChemin.Arrivee - 1]}) :");
                            graphe.Dijkstra(nomsGares[cmdChemin.Depart - 1], nomsGares[cmdChemin.Arrivee - 1], nomsGares);
                            break;
                        case "0":
                            exitCmd = true;
                            break;
                        default:
                            Console.WriteLine("Option non reconnue.");
                            break;
                    }
                }

                BilanRepository bilanRepo = new BilanRepository(connectionString);
                bool exitBilans = false;
                while (!exitBilans)
                {
                    Console.WriteLine("\n--- Bilans Généraux ---");
                    Console.WriteLine("1. Afficher par cuisinier le nombre de livraisons effectuées");
                    Console.WriteLine("2. Afficher les commandes selon une période de temps");
                    Console.WriteLine("3. Afficher la moyenne des prix des commandes");
                    Console.WriteLine("4. Afficher la moyenne des achats des clients");
                    Console.WriteLine("5. Afficher la liste des commandes pour un client (filtré par nationalité et période)");
                    Console.WriteLine("0. Quitter");
                    Console.Write("Votre choix : ");
                    string choixBilans = Console.ReadLine();
                    switch (choixBilans)
                    {
                        case "1":
                            bilanRepo.AfficherLivraisonsParCuisinier();
                            break;
                        case "2":
                            Console.Write("Date de début (AAAA-MM-JJ) : ");
                            DateTime? debut = null;
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime d))
                                debut = d;
                            Console.Write("Date de fin (AAAA-MM-JJ) : ");
                            DateTime? fin = null;
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime f))
                                fin = f;
                            List<Commande> cmds = bilanRepo.ObtenirCommandesParPeriode(debut, fin);
                            Console.WriteLine("Commandes dans la période :");
                            foreach (var c in cmds)
                            {
                                Console.WriteLine($"Commande {c.IdCommande}, Date: {c.DateCommande.ToShortDateString()}, Cout: {c.CoutTotal}");
                            }
                            break;
                        case "3":
                            double moyennePrix = bilanRepo.CalculerMoyennePrixCommandes();
                            Console.WriteLine($"Moyenne des prix des commandes : {moyennePrix}");
                            break;
                        case "4":
                            double moyenneAchats = bilanRepo.CalculerMoyenneAchatsClients();
                            Console.WriteLine($"Moyenne des achats des clients : {moyenneAchats}");
                            break;
                        case "5":
                            Console.Write("ID du client : ");
                            string idClient = Console.ReadLine();
                            Console.Write("Nationalité des plats (laisser vide pour tous) : ");
                            string pays = Console.ReadLine();
                            Console.Write("Date de début (AAAA-MM-JJ) : ");
                            DateTime? debut2 = null;
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime d2))
                                debut2 = d2;
                            Console.Write("Date de fin (AAAA-MM-JJ) : ");
                            DateTime? fin2 = null;
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime f2))
                                fin2 = f2;
                            bilanRepo.AfficherCommandesPourClient(idClient, pays, debut2, fin2);
                            break;
                        case "0":
                            exitBilans = true;
                            break;
                        default:
                            Console.WriteLine("Option non reconnue.");
                            break;
                    }
                }

                StatistiquesRepository statsRepo = new StatistiquesRepository(connectionString);
                bool exitStats = false;
                while (!exitStats)
                {
                    Console.WriteLine("\n--- Statistiques globales ---");
                    Console.WriteLine("1. Cuisiniers au‑dessus de la moyenne de livraisons");
                    Console.WriteLine("2. Clients sans aucune commande");
                    Console.WriteLine("3. Plats les plus chers de leur pays");
                    Console.WriteLine("4. Clients ayant au moins une commande > 100€");
                    Console.WriteLine("5. Plats jamais commandés");
                    Console.WriteLine("0. Retour");
                    Console.Write("Votre choix : ");
                    string choixStats = Console.ReadLine();
                    switch (choixStats)
                    {
                        case "1":
                            statsRepo.AfficherCuisiniersAuDessusDeLaMoyenne();
                            break;
                        case "2":
                            statsRepo.AfficherClientsSansCommandes();
                            break;
                        case "3":
                            statsRepo.AfficherPlatsLesPlusChersParPays();
                            break;
                        case "4":
                            statsRepo.AfficherClientsAvecCommandesHautPrix();
                            break;
                        case "5":
                            statsRepo.AfficherPlatsJamaisCommandés();
                            break;
                        case "0":
                            exitStats = true;
                            break;
                        default:
                            Console.WriteLine("Option non reconnue.");
                            break;
                    }
                }

                Console.WriteLine("\nAppuyez sur une touche pour quitter...");
            Console.ReadKey();
        }
    }
}
    }
