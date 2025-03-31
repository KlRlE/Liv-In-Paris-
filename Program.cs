using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SkiaSharp;

namespace LivInParis
{
    // Classe générique représentant un nœud (par exemple, une station)
    public class Noeud<T>
    {
        public T Id { get; set; }
        public List<T> Connexions { get; set; } = new List<T>();
    }

    // Classe générique représentant une liaison entre deux nœuds avec un poids (temps de trajet)
    public class Lien<T>
    {
        public T N1 { get; set; }
        public T N2 { get; set; }
        public double Poids { get; set; }  // Poids pour représenter le temps de trajet
    }

    // Classe générique pour un graphe
    public class Graphe<T>
    {
        // Dictionnaire qui associe un identifiant de nœud à son objet Noeud<T>
        public Dictionary<T, Noeud<T>> Nodes { get; private set; } = new Dictionary<T, Noeud<T>>();

        // Liste des liaisons (arêtes) du graphe
        public List<Lien<T>> Liens { get; private set; } = new List<Lien<T>>();

        /// <summary>
        /// Constructeur qui prend la liste des nœuds (identifiants) et la liste des liaisons
        /// </summary>
        public Graphe(IEnumerable<T> noeuds, IEnumerable<Lien<T>> liens)
        {
            foreach (var id in noeuds)
            {
                Nodes[id] = new Noeud<T> { Id = id };
            }
            foreach (var lien in liens)
            {
                if (Nodes.ContainsKey(lien.N1) && Nodes.ContainsKey(lien.N2))
                {
                    Nodes[lien.N1].Connexions.Add(lien.N2);
                    Nodes[lien.N2].Connexions.Add(lien.N1);
                    Liens.Add(lien);
                }
            }
        }

        /// <summary>
        /// Affiche la liste d’adjacence.
        /// </summary>
        public void Afficher_L_Adjacence()
        {
            foreach (var node in Nodes.Values)
            {
                Console.Write($"{node.Id}: ");
                Console.WriteLine(string.Join(", ", node.Connexions));
            }
        }



        

       

        /// <summary>
        /// Génère une image du graphe avec SkiaSharp.
        /// </summary>
        public void GenererImageGraphe(string chemin)
        {
            int largeur = 1000, hauteur = 1000;
            Random rand = new Random();
            using (var surface = SKSurface.Create(new SKImageInfo(largeur, hauteur)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                SKPaint edgePaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 };
                SKPaint nodePaint = new SKPaint { Color = SKColors.Blue, StrokeWidth = 2 };
                SKPaint textPaint = new SKPaint { Color = SKColors.Black, TextSize = 24, IsAntialias = true };

                double centerX = largeur / 2.0, centerY = hauteur / 2.0;

                // Choisir le nœud central (celui ayant le plus de connexions)
                var centralNode = Nodes.Values.OrderByDescending(n => n.Connexions.Count).First();
                var outerNodes = Nodes.Values.Where(n => !n.Id.Equals(centralNode.Id)).ToList();

                float centralNodeRadius = 40;
                float outerNodeRadiusBase = 20;
                float minDistance = 50;

                Dictionary<T, SKPoint> nodePositions = new Dictionary<T, SKPoint>();
                nodePositions[centralNode.Id] = new SKPoint((float)centerX, (float)centerY);

                // Fonction locale pour vérifier le chevauchement
                bool CheckForOverlap(SKPoint newPosition, Dictionary<T, SKPoint> existingPositions, float radius)
                {
                    foreach (var pos in existingPositions.Values)
                    {
                        float distance = (float)Math.Sqrt(Math.Pow(newPosition.X - pos.X, 2) + Math.Pow(newPosition.Y - pos.Y, 2));
                        if (distance < radius + minDistance)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                // Placement aléatoire des autres nœuds sans chevauchement
                foreach (var outer in outerNodes)
                {
                    SKPoint newPosition;
                    bool overlap;
                    do
                    {
                        float x = (float)(rand.NextDouble() * largeur);
                        float y = (float)(rand.NextDouble() * hauteur);
                        newPosition = new SKPoint(x, y);
                        overlap = CheckForOverlap(newPosition, nodePositions, outerNodeRadiusBase);
                    } while (overlap);
                    nodePositions[outer.Id] = newPosition;
                }

                // Dessiner les arêtes (on ne dessine chaque arête qu'une seule fois)
                HashSet<string> drawnEdges = new HashSet<string>();
                foreach (var node in Nodes.Values)
                {
                    foreach (var voisin in node.Connexions)
                    {
                        string edgeKey = string.Compare(node.Id.ToString(), voisin.ToString()) < 0 ?
                            node.Id.ToString() + "-" + voisin.ToString() :
                            voisin.ToString() + "-" + node.Id.ToString();
                        if (!drawnEdges.Contains(edgeKey))
                        {
                            if (nodePositions.ContainsKey(node.Id) && nodePositions.ContainsKey(voisin))
                            {
                                canvas.DrawLine(nodePositions[node.Id], nodePositions[voisin], edgePaint);
                            }
                            drawnEdges.Add(edgeKey);
                        }
                    }
                }

                // Dessiner les nœuds
                foreach (var node in Nodes.Values)
                {
                    float nodeSize = Math.Max(outerNodeRadiusBase, 5 + 2 * node.Connexions.Count);
                    var position = nodePositions[node.Id];
                    canvas.DrawCircle(position, nodeSize, nodePaint);
                    canvas.DrawText(node.Id.ToString(), position.X - 10, position.Y + 10, textPaint);
                }

                // Sauvegarder l'image dans un fichier
                using (var img = surface.Snapshot())
                using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(chemin))
                {
                    data.SaveTo(stream);
                }
            }
        }

        
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Pour la démonstration, nous simulons une partie du plan du métro de Paris
            // avec des temps de trajet (en minutes) associés à chaque liaison.
            List<string> stations = new List<string>
            {
                "Châtelet",
                "Gare du Nord",
                "Saint-Lazare",
                "Montparnasse",
                "Bastille",
                "République",
                "Opéra",
                "Nation"
            };

            // Définition des liaisons entre stations avec leur temps de trajet
            List<Lien<string>> liens = new List<Lien<string>>
            {
                new Lien<string>{ N1 = "Châtelet", N2 = "Gare du Nord", Poids = 5 },
                new Lien<string>{ N1 = "Châtelet", N2 = "Saint-Lazare", Poids = 7 },
                new Lien<string>{ N1 = "Châtelet", N2 = "Opéra", Poids = 4 },
                new Lien<string>{ N1 = "Gare du Nord", N2 = "République", Poids = 6 },
                new Lien<string>{ N1 = "Saint-Lazare", N2 = "Montparnasse", Poids = 8 },
                new Lien<string>{ N1 = "Montparnasse", N2 = "Bastille", Poids = 5 },
                new Lien<string>{ N1 = "Bastille", N2 = "Nation", Poids = 10 },
                new Lien<string>{ N1 = "Opéra", N2 = "République", Poids = 3 },
                new Lien<string>{ N1 = "République", N2 = "Nation", Poids = 4 }
            };

            // Création du graphe générique à partir des stations et liaisons
            Graphe<string> metroGraph = new Graphe<string>(stations, liens);

            Console.WriteLine("Liste d'adjacence du plan du métro de Paris :");
            metroGraph.Afficher_L_Adjacence();

            // Génération de l'image du graphe
            string imagePath = "metroGraph.png";
            metroGraph.GenererImageGraphe(imagePath);
            Console.WriteLine($"Image du graphe sauvegardée sous {imagePath}");

          
            var trajetFinal = metroGraph.ReconstruireChemin("Châtelet", "Nation", Pred);
            Console.WriteLine("Trajet final de Châtelet à Nation : " + string.Join(" -> ", trajetFinal));




            Console.WriteLine("Appuyez sur une touche pour quitter.");
            Console.ReadKey();
        }
    }
}
