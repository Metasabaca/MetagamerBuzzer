using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetagamerBuzzer
{
    class Question : Element
    {
        public string question;

        // Constructor: 
        public Question(int num, string question, string reponse, int points)
        {
            this.num = num;
            this.question = question;
            this.reponse = reponse;
            this.points = points;
        }
    }
}
