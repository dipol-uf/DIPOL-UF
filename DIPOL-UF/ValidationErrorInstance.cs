using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF
{
    class ValidationErrorInstance  : IEquatable<ValidationErrorInstance>
    {
       
        public string Message
        {
            get;
            private set;

        }

        public string ID
        {
            get;
            private set;
        }

        public ValidationErrorInstance(string id, string message)
        {
            Message = message;
            ID = id;
        }

        public bool Equals(ValidationErrorInstance other)
            => this.ID == other.ID;

        public override bool Equals(object obj)
            => obj is ValidationErrorInstance inst ? inst.ID == this.ID : false;

        public override int GetHashCode()
            => this.ID.GetHashCode();
        public override string ToString()
            => ID;
    }
}
