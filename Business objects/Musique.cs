using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetagamerBuzzer
{
    class Musique : Element
    {
        public string indice;

        // Constructor: 
        public Musique(int num, string indice, string reponse, int points)
        {
            this.num = num;
            this.indice = indice;
            this.reponse = reponse;
            this.points = points;
        }
    }
}
