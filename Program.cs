using SkiaSharp;

namespace LivInParis
{
    internal class Program
    {

        public  class Noeud
        {
            public int Numéro { get ; set; }
            public List<int> Connexion { get ; set; } = new List<int>();
        }
        public class  Lien
        {
            public int N1 { get; set; }
            public int N2 { get; set; }
        }
        public class  Graphe
        {
            public List<Noeud>  L_Adjacence { get; set; } = new List<Noeud>();
            public int[,]  M_Adjacence { get; set; }
            public int  N_Noeuds { get; set; }

            public Graphe(int n_Noeuds , List<Lien> liens)
            {
                N_Noeuds = n_Noeuds;
                M_Adjacence = new int[n_Noeuds + 1, n_Noeuds + 1];

                for (int i = 1 ; i <= n_Noeuds; i++)
                {
                    L_Adjacence.Add(new Noeud { Numéro = i });
                }
                foreach (var  lien in liens)
                {
                    L_Adjacence[lien.N1 - 1].Connexion.Add(lien.N2);
                    L_Adjacence[lien.N2 - 1].Connexion.Add(lien.N1);

                    M_Adjacence[lien.N1, lien.N2] = 1;
                    M_Adjacence[lien.N2, lien.N1] = 1;
                }
            }
            public void  Afficher_L_Adjacence()
            {
                foreach  (var noeud in L_Adjacence)
                {
                    Console.Write($"{noeud.Numéro}: ");
                    Console.WriteLine(string.Join(", ", noeud.Connexion));
                    /*Console.Write(noeud.Numéro + ": ");
                    foreach (var Connexion in noeud.Connexion) 
                    {
                        Console.Write(noeud.Connexion + " ;");
                    }
                    Console.WriteLine("");*/
                }
            }

             
            public void Afficher_M_Adjacence()
            {
                for (int i = 1;  i <= N_Noeuds; i++)
                {
                    for (int j = 1;  j <= N_Noeuds; j++)
                    {
                        Console.Write(M_Adjacence[i, j] + " ");
                    }
                    Console.WriteLine();
                }
            }

            /// <summary>
            /// effectue le parcours avec add et laisser la priorité
            /// </summary>
            /// <param name="depart"></param>
            /// <returns></returns>
            public List<int> ParcoursLargeur_Travail(int depart)
            {
                Queue<int> file = new Queue<int>();
                HashSet<int> visite = new HashSet<int>();
                List<int> res = new List<int>();
                file.Enqueue(depart);
                visite.Add(depart);


                while (file.Count >  0)
                {
                    int noeud = file.Dequeue();
                    res.Add(noeud);


                    foreach (var voisin in L_Adjacence[noeud - 1].Connexion)
                    {
                        if (!visite.Contains(voisin))
                        {
                            file.Enqueue(voisin);
                            visite.Add(voisin);
                        }
                    }
                }
                return res;
            }
            /// <summary>
            /// affiche le resulat de ParcoursLargeur_Travail
            /// </summary>
            /// <param name="depart"></param>
            public void  ParcoursLargeur(int depart)
            {
                Console.Write("Parcours longueur" + " ");
                List<int> res = new List<int>();
                for (int i = 0; i < ParcoursLargeur_Travail(depart).Count; i++)
                {
                    res.Add(ParcoursLargeur_Travail(depart)[i]);
                    Console.Write(res[i] + " ");
                }
                Console.WriteLine("");
            }

            public bool  Est_Connexe()
            {
                return ParcoursLargeur_Travail(1).Count == N_Noeuds;
            }

            /// <summary>
            /// genere l'image
            /// </summary>
            /// <param name="chemin"></param>
            public void  GenererImageGraphe(string chemin)
            {
                int largeur =1000 , height = 1000;
                Random rand = new Random(); 
                using (var surface = SKSurface.Create(new SKImageInfo(largeur, height)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    SKPaint edgePaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 };
                    SKPaint nodePaint = new SKPaint { Color = SKColors.Blue, StrokeWidth = 2 };
                    SKPaint textPaint = new SKPaint { Color = SKColors.White, TextSize = 24, IsAntialias = true };

                    
                    double centerX = largeur / 2, centerY = height / 2;

                    
                    Noeud centralNode = L_Adjacence.OrderByDescending(n => n.Connexion.Count).First();
                    List<Noeud> outerNodes = L_Adjacence.Where(n => n != centralNode).ToList();

                    
                    float centralNodeRadius = 40;
                    float outerNodeRadiusBase = 20; 
                    float minDistance = 50;

                    
                    Dictionary<int, SKPoint> nodePositions = new Dictionary<int, SKPoint>();

                    
                    nodePositions[centralNode.Numéro] = new SKPoint((float)centerX, (float)centerY);

                   
                    bool CheckForOverlap(SKPoint newPosition, Dictionary<int, SKPoint> existingPositions, float radius)
                    {
                        foreach (var position in existingPositions.Values)
                        {
                            float distance = (float)Math.Sqrt(Math.Pow(newPosition.X - position.X, 2) + Math.Pow(newPosition.Y - position.Y, 2));
                            if (distance < radius + minDistance)
                            {
                                return true; 
                            }
                        }
                        return false; 
                    }

                    
                    foreach (var outerNode in outerNodes)
                    {
                        SKPoint newPosition;
                        bool overlap;
                        do
                        {
                            float x = (float)(rand.NextDouble() * largeur); 
                            float y = (float)(rand.NextDouble() * height);
                            newPosition = new SKPoint(x, y);

                            
                            overlap = CheckForOverlap(newPosition, nodePositions, outerNodeRadiusBase);
                        } while (overlap); 

                        nodePositions[outerNode.Numéro] = newPosition;
                    }

                    
                    foreach (var noeud in L_Adjacence)
                    {
                        foreach (var voisin in noeud.Connexion)
                        {
                            if (noeud.Numéro < voisin)
                            {
                                canvas.DrawLine(nodePositions[noeud.Numéro], nodePositions[voisin], edgePaint);
                            }
                        }
                    }

                    
                    foreach (var noeud in L_Adjacence)
                    {
                        
                        float nodeSize = Math.Max(outerNodeRadiusBase, 5 + 2 * noeud.Connexion.Count);
                        var position = nodePositions[noeud.Numéro];
                        canvas.DrawCircle(position, nodeSize, nodePaint);
                        canvas.DrawText(  noeud.Numéro.ToString(), position.X - 10, position.Y + 10, textPaint);
                    }

                    // Save the image to a file
                    using (var img = surface.Snapshot())
                    using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                    using (var stream = File.OpenWrite(chemin))
                    {
                        data.SaveTo(stream);
                    }
                }
            }


         
   


            /// <summary>
            /// parcours en profondeur avec push pour suivre directement
            /// </summary>
            /// <param name="depart"></param>
        public void ParcoursProfondeur(int depart)
            {
                Stack<int> pile = new Stack<int>();
                HashSet<int> visite = new HashSet<int>();
                pile.Push(depart);

                Console.Write("Parcours en profondeur : ");
                while (pile.Count > 0)
                {
                    int noeud = pile.Pop();
                    if (!visite.Contains(noeud))
                    {
                        Console.Write(noeud + " ");
                        visite.Add(noeud);
                    }

                    foreach (var voisin in L_Adjacence[noeud - 1].Connexion)
                    {
                        if (!visite.Contains(voisin))
                        {
                            pile.Push(voisin);
                        }
                    }
                }
                
            }

        }

        
        

        static void Main(string[] args)
        {

            List<Lien> liens = new List<Lien>();
            int nombreNoeuds = 0;

            // Lecture du fichier soc-karate.mtx
            string path = "soc-karate.mtx";
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("%")) continue; // Ignorer les commentaires
                string[] parts = line.Split();
                if (parts.Length != 2)
                {
                    nombreNoeuds = int.Parse(parts[0]);
                }
                else if (parts.Length == 2)
                {
                    int noeud1 = int.Parse(parts[0]);
                    int noeud2 = int.Parse(parts[1]);
                    liens.Add(new Lien { N1 = noeud1, N2 = noeud2 });
                }
            }

            Graphe graphe = new Graphe(nombreNoeuds, liens);
            Console.WriteLine("Liste d'adjacence:");
            graphe.Afficher_L_Adjacence();
            if (graphe.Est_Connexe()) 
            {
                Console.WriteLine("Graphe Connexe");
            }
            else
            {
                Console.WriteLine("Graohe non Connexe");
            }
            for (int i = 1; i < nombreNoeuds; i++) 
            {
                graphe.ParcoursLargeur(i);
            }

            Graphe graphedessin = new Graphe(nombreNoeuds, liens);
            string imagePath = "graphe.png";
            graphe.GenererImageGraphe(imagePath);
            Console.WriteLine($"Graph image saved to {imagePath}");

            Console.WriteLine("\nMatrice d'adjacence:");
            graphe.Afficher_M_Adjacence();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
