using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using SkiaSharp;

namespace LIP
{
    internal class Program
    {
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

                // si matrice
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

            // poids total
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

                // Intitialisation(infini
                for ( int i = 1; i <= N_Noeuds; i++)
                {
                    distances[i]  = 99999;
                    nonVisites.Add(i );
                }



                distances[idDepart] =  0;
                 
                while (  nonVisites.Count > 0)
                {
                    // Noeud plus proche
                    int N_actuel  = nonVisites.OrderBy(n => distances[n]).First();
                    nonVisites.Remove(N_actuel );

                    //Voisin direct
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

                // Initalise distances à l 'infini
                for (int i = 1; i <=  N_Noeuds; i++)
                    distances[i] =  99999;

                distances[idDepart] = 0;

                // Relaxer les arêtes plusieurs fois
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


                 // Cycle ---- ?
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

                 // Indices des gares
                int idDepart  = nomsGares.FindIndex(nom => nom.Equals(nomDepart, StringComparison.OrdinalIgnoreCase)) + 1;
                int idArrivee =  nomsGares.FindIndex(nom => nom.Equals(nomArrivee, StringComparison.OrdinalIgnoreCase)) + 1;

                if (idDepart <= 0 || idArrivee <= 0 || suivant[idDepart, idArrivee] == -1)
                {
                    Console.WriteLine(" Pas de chemin possible entre ces deux gares.");
                    return;
                }

                 // Fabric chemaiin
                List<int> chemin =  new List<int> { idDepart };
                int actuel  = idDepart;

                while (actuel  != idArrivee)
                {
                    actuel =  suivant[actuel, idArrivee];
                    chemin.Add( actuel);
                }

                // Affichage 
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






            /// <summary>
            /// génére le graphe
            /// </summary>
            /// <param name="chemin"></param>
            /// <param name="nomsGares"></param>
            public void GenererImageGraphe(string chemin, List<string> nomsGares)
            {
                int largeur = 3500, hauteur = 3500;
                Random rand = new Random();

                using (var surface = SKSurface.Create(new SKImageInfo(largeur, hauteur)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    SKPaint edgePaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 };
                    SKPaint nodePaint = new SKPaint { Color = SKColors.Blue, StrokeWidth = 2 };
                    SKPaint textPaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        TextSize = 36, 
                        IsAntialias = true
                    };
                    SKPaint weightPaint = new SKPaint
                    {
                        Color = SKColors.Red,
                        TextSize = 28, 
                        IsAntialias = true
                    };

                    if (L_Adjacence.Count == 0)
                    {
                        Console.WriteLine("Rien à faire");
                        return;
                    }

                    Noeud centralNode = L_Adjacence.OrderByDescending(n => n.Connexion.Count).First();
                    List<Noeud> outerNodes = L_Adjacence.Where(n => n != centralNode).ToList();

                    float radiusBase = 40; 
                    float minDistance = 100;
                    Dictionary<int, SKPoint> nodePositions = new Dictionary<int, SKPoint>();

                    nodePositions[centralNode.Numéro] = new SKPoint(largeur / 2f, hauteur / 2f);

                    bool CheckOverlap(SKPoint newPos, float radius)
                    {
                        foreach (var pos in nodePositions.Values)
                        {
                            float dist = (float)Math.Sqrt(Math.Pow(newPos.X - pos.X, 2) + Math.Pow(newPos.Y - pos.Y, 2));
                            if (dist < radius + minDistance) return true;
                        }
                        return false;
                    }

                    foreach (var node in outerNodes)
                    {
                        SKPoint pos;
                        do
                        {
                            float x =  (float)(rand.NextDouble() * largeur);
                            float y  = (float)(rand.NextDouble() * hauteur);
                            pos = new SKPoint(x, y);
                        } while  (CheckOverlap(pos, radiusBase));

                        nodePositions[node.Numéro] = pos;
                    }

                    // Dessin des liens
                     foreach (var noeud in L_Adjacence)
                    {
                        foreach (var voisin in noeud.Connexion)
                        {
                            if (noeud.Numéro < voisin)
                            {
                                 canvas.DrawLine(nodePositions[noeud.Numéro], nodePositions[voisin], edgePaint);

                                 float midX = (nodePositions[noeud.Numéro].X + nodePositions[voisin].X) / 2;
                                float midY = (nodePositions[noeud.Numéro].Y + nodePositions[voisin].Y) / 2;
                                 int poids = M_Adjacence[noeud.Numéro, voisin];
                                canvas.DrawText(poids.ToString(), midX, midY, weightPaint);
                            }
                        }
                    }

                    // Dessin des nœuds + texte
                    foreach (var noeud in L_Adjacence)
                    {
                        float size  = Math.Max(radiusBase, 10 + 4 * noeud.Connexion.Count);
                        var pos =  nodePositions[noeud.Numéro];
                        canvas.DrawCircle(pos, size, nodePaint);

                        string nomGare =  (nomsGares != null && noeud.Numéro <= nomsGares.Count)
                            ? nomsGares[noeud.Numéro - 1]
                            : noeud.Numéro.ToString();

                        // Centrage amélioré
                        float offsetX =  nomGare.Length * 12;
                        canvas.DrawText(nomGare, pos.X - offsetX / 2, pos.Y + 12, textPaint);
                    }

                    using  (var img = surface.Snapshot())
                    using  (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                    using  (var stream = File.OpenWrite(chemin))
                    {
                         data.SaveTo(stream);
                    }

                    Console.WriteLine( "Image enregistré");
                }
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
            var liensAjoutes = new HashSet<string>(); // évite doublons

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

                        string nomVoisine = feuille.Cells[numVoisine + 1, 2].Text.Trim(); // Excel commence à ligne 2

                        if (string.IsNullOrWhiteSpace(nomVoisine)) return;

                        if (!nomToId.ContainsKey(nomVoisine))
                        {
                            nomToId[nomVoisine] = nomToId.Count + 1;
                            nomsGares.Add(nomVoisine);
                        }

                        int idDest = nomToId[nomVoisine];

                        // Evite doublon
                        string cleLien = $"{Math.Min(idGare, idDest)}-{Math.Max(idGare, idDest)}";

                        if (!liensAjoutes.Contains(cleLien))
                        {
                            liens.Add(new Program.Lien { N1 = idGare, N2 = idDest, Poids = temps });
                            liensAjoutes.Add(cleLien);
                        }
                    }

                    AjouterLien(precedent);
                    AjouterLien(suivant);

                    // Correspondance
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


            string imagePath = "reseau_gares.png";
            graphe.GenererImageGraphe(imagePath, nomsGares);
            Console.WriteLine($"\nImage générée : {imagePath}");

            Console.WriteLine("\nMatrice d'adjacence :");
            graphe.Afficher_M_Adjacence();

            graphe.Dijkstra("Châtelet", "Place de Clichy", nomsGares);
            graphe.BellmanFord("Châtelet", "Nation", nomsGares);
            graphe.FloydWarshall("Châtelet", "Place de Clichy", nomsGares);



            Console.WriteLine("\nAppuyez sur une touche pour quitter...");
            Console.ReadKey();
        }
    }
}
