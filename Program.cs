using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace LivInParis
{
    // Représente un nœud générique (par exemple, une station)
    public class Noeud<T>
    {
        public T Id { get; set; }
        public List<T> Connexions { get; set; } = new List<T>();
    }

    // Représente une liaison (arête) entre deux nœuds avec un poids (par exemple, temps de trajet)
    public class Lien<T>
    {
        public T N1 { get; set; }
        public T N2 { get; set; }
        public double Poids { get; set; }
    }

    // Représente un graphe générique
    public class Graphe<T>
    {
        public Dictionary<T, Noeud<T>> Nodes { get; private set; } = new Dictionary<T, Noeud<T>>();
        public List<Lien<T>> Liens { get; private set; } = new List<Lien<T>>();

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
                    // Graphe non orienté : on ajoute la connexion dans les deux sens
                    Nodes[lien.N1].Connexions.Add(lien.N2);
                    Nodes[lien.N2].Connexions.Add(lien.N1);
                    Liens.Add(lien);
                }
            }
        }

        /// <summary>
        /// Algorithme de Dijkstra.
        /// Retourne un tuple (Dist, Pred) contenant :
        /// - Dist : la distance minimale depuis la source s pour chaque nœud.
        /// - Pred : le prédécesseur de chaque nœud dans le chemin optimal.
        /// </summary>
        public (Dictionary<T, double> Dist, Dictionary<T, T> Pred) Dijkstra(T s)
        {
            var Dist = new Dictionary<T, double>();
            var Pred = new Dictionary<T, T>();
            var nonVisites = new HashSet<T>(Nodes.Keys);

            foreach (var noeud in Nodes.Keys)
            {
                Dist[noeud] = double.PositiveInfinity;
                Pred[noeud] = default(T);
            }
            Dist[s] = 0;

            while (nonVisites.Count > 0)
            {
                T u = nonVisites.OrderBy(x => Dist[x]).First();
                nonVisites.Remove(u);

                if (double.IsPositiveInfinity(Dist[u]))
                    break;

                foreach (var v in Nodes[u].Connexions)
                {
                    if (!nonVisites.Contains(v))
                        continue;

                    double w = Liens
                        .First(l => (l.N1.Equals(u) && l.N2.Equals(v)) ||
                                    (l.N1.Equals(v) && l.N2.Equals(u)))
                        .Poids;

                    double alt = Dist[u] + w;
                    if (alt < Dist[v])
                    {
                        Dist[v] = alt;
                        Pred[v] = u;
                    }
                }
            }
            return (Dist, Pred);
        }

        /// <summary>
        /// Algorithme de Bellman–Ford.
        /// Permet de trouver le chemin le plus court depuis la source, même en présence d'arêtes à poids négatif.
        /// Retourne un tuple (Dist, Pred).
        /// </summary>
        public (Dictionary<T, double> Dist, Dictionary<T, T> Pred) BellmanFord(T source)
        {
            var dist = new Dictionary<T, double>();
            var pred = new Dictionary<T, T>();

            // Initialisation
            foreach (var node in Nodes.Keys)
            {
                dist[node] = double.PositiveInfinity;
                pred[node] = default(T);
            }
            dist[source] = 0;

            int V = Nodes.Count;
            // Relaxer toutes les arêtes V-1 fois
            for (int i = 1; i < V; i++)
            {
                foreach (var edge in Liens)
                {
                    // Relaxation dans les deux sens (graphe non orienté)
                    if (dist[edge.N1] + edge.Poids < dist[edge.N2])
                    {
                        dist[edge.N2] = dist[edge.N1] + edge.Poids;
                        pred[edge.N2] = edge.N1;
                    }
                    if (dist[edge.N2] + edge.Poids < dist[edge.N1])
                    {
                        dist[edge.N1] = dist[edge.N2] + edge.Poids;
                        pred[edge.N1] = edge.N2;
                    }
                }
            }

            // Vérifier la présence de cycles de poids négatif
            foreach (var edge in Liens)
            {
                if (dist[edge.N1] + edge.Poids < dist[edge.N2] ||
                    dist[edge.N2] + edge.Poids < dist[edge.N1])
                {
                    throw new Exception("Le graphe contient un cycle de poids négatif");
                }
            }

            return (dist, pred);
        }

        /// <summary>
        /// Reconstruit le chemin optimal de la source à la destination à partir du dictionnaire des prédécesseurs.
        /// </summary>
        public List<T> ReconstruireChemin(T source, T destination, Dictionary<T, T> pred)
        {
            var chemin = new List<T>();
            T courant = destination;
            while (!courant.Equals(source))
            {
                chemin.Add(courant);
                if (pred[courant] == null || pred[courant].Equals(default(T)))
                {
                    Console.WriteLine("Aucun chemin trouvé.");
                    return new List<T>();
                }
                courant = pred[courant];
            }
            chemin.Add(source);
            chemin.Reverse();
            return chemin;
        }

        /// <summary>
        /// Affiche le trajet final en indiquant pour chaque segment le poids et le poids total du trajet.
        /// </summary>
        public void AfficherTrajetAvecPoids(List<T> trajet)
        {
            if (trajet == null || trajet.Count == 0)
            {
                Console.WriteLine("Aucun trajet à afficher.");
                return;
            }

            double poidsTotal = 0;
            Console.Write($"{trajet[0]}");
            for (int i = 0; i < trajet.Count - 1; i++)
            {
                double poidsSegment = Liens
                    .First(l => (l.N1.Equals(trajet[i]) && l.N2.Equals(trajet[i + 1])) ||
                                (l.N1.Equals(trajet[i + 1]) && l.N2.Equals(trajet[i])))
                    .Poids;
                poidsTotal += poidsSegment;
                Console.Write($" --({poidsSegment})--> {trajet[i + 1]}");
            }
            Console.WriteLine();
            Console.WriteLine($"Poids total du trajet : {poidsTotal}");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Exemple de stations du métro de Paris
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

            // Liaisons avec poids (temps de trajet en minutes)
            List<Lien<string>> liens = new List<Lien<string>>
            {
                new Lien<string>{ N1 = "Châtelet",     N2 = "Gare du Nord",   Poids = 5 },
                new Lien<string>{ N1 = "Châtelet",     N2 = "Saint-Lazare",   Poids = 7 },
                new Lien<string>{ N1 = "Châtelet",     N2 = "Opéra",          Poids = 4 },
                new Lien<string>{ N1 = "Gare du Nord", N2 = "République",     Poids = 6 },
                new Lien<string>{ N1 = "Saint-Lazare", N2 = "Montparnasse",   Poids = 8 },
                new Lien<string>{ N1 = "Montparnasse", N2 = "Bastille",       Poids = 5 },
                new Lien<string>{ N1 = "Bastille",     N2 = "Nation",         Poids = 10 },
                new Lien<string>{ N1 = "Opéra",        N2 = "République",     Poids = 3 },
                new Lien<string>{ N1 = "République",   N2 = "Nation",         Poids = 4 }
            };

            // Création du graphe
            Graphe<string> metroGraph = new Graphe<string>(stations, liens);

            // --- Dijkstra ---
            var (distDijkstra, predDijkstra) = metroGraph.Dijkstra("Châtelet");
            var cheminDijkstra = metroGraph.ReconstruireChemin("Châtelet", "Nation", predDijkstra);
            Console.WriteLine("=== Dijkstra ===");
            metroGraph.AfficherTrajetAvecPoids(cheminDijkstra);

            // --- Bellman–Ford ---
            var (distBellmanFord, predBellmanFord) = metroGraph.BellmanFord("Châtelet");
            var cheminBellmanFord = metroGraph.ReconstruireChemin("Châtelet", "Nation", predBellmanFord);
            Console.WriteLine("\n=== Bellman–Ford ===");
            metroGraph.AfficherTrajetAvecPoids(cheminBellmanFord);         

            Console.WriteLine("\nAppuyez sur une touche pour quitter.");
            Console.ReadKey();
        }
    }
}
