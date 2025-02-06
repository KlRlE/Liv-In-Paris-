namespace LivInParis
{
    internal class Program
    {
        
        public class Noeud 
        {
            public int Numéro {  get; set; }
            public List<int> Connexion { get; set; }= new List<int>();
        }
        public class  Lien 
        {
            public int N1 { get; set; }
            public int N2 { get; set; }
        }
        public class Graphe 
        {
            public List<Noeud> L_Adjacence { get; set; }=new List<Noeud>();
            public int[,] M_Adjacence { get;set; }
            public int N_Noeuds {  get; set; }

            public Graphe(int n_Noeuds,List<Lien> liens) 
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

                    M_Adjacence[lien.N1, lien.N2] = 1;
                    M_Adjacence[lien.N2, lien.N1] = 1;
                }
            }
            public void Afficher_L_Adjacence() 
            {
                foreach(var noeud in L_Adjacence)
                {
                    /*Console.Write($"{noeud.Numéro}: ");
                    Console.WriteLine(string.Join(", ", noeud.Connexion));*/
                    Console.Write(noeud.Numéro + ": ");
                    foreach (var Connexion in noeud.Connexion) 
                    {
                        Console.Write(noeud.Connexion + " ;");
                    }
                    Console.WriteLine("");
                }
            }

            public void Afficher_M_Adjacence() 
            {
                for (int i = 1;i<= N_Noeuds; i++) 
                {
                    for(int j=1;j<= N_Noeuds; j++) 
                    {
                        Console.Write(M_Adjacence[i, j] + " ");
                    }
                    Console.WriteLine();
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

            Console.WriteLine("\nMatrice d'adjacence:");
            graphe.Afficher_M_Adjacence();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
