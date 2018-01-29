using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF
{
    public class ValidationErrorInstance  : IEquatable<ValidationErrorInstance>
    {
       
        public string Message
        {
            get;
        }

        public string ID
        {
            get;
        }

        public ValidationErrorInstance(string id, string message)
        {
            Message = message;
            ID = id;
        }

        public bool Equals(ValidationErrorInstance other)
            => ID == other?.ID;

        public override bool Equals(object obj)
            => obj is ValidationErrorInstance inst && inst.ID == ID;

        public override int GetHashCode()
            => ID.GetHashCode();
        public override string ToString()
            => ID;
    }
}
