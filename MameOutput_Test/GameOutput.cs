using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MameOutput_Test
{
    /// <summary>
    /// Définition d'une Output : Nom / ID / Value
    /// </summary>
    public class GameOutput
    {
        protected String _Name;
        protected UInt32 _Id;
        protected int _OutputValue;

        public String Name
        { get { return _Name; } }

        public UInt32 Id
        { get { return _Id; } }

        public virtual int OutputValue
        {
            get { return _OutputValue; }
            set { _OutputValue = value; }
        }

        public GameOutput(String Name, UInt32 Id)
        {
            _Name = Name;
            _Id = Id;
            _OutputValue = 0;
        }

        public GameOutput(GameOutput Output)
        {
            _Name = Output.Name;
            _Id = Output.Id;
            _OutputValue = Output.OutputValue;
        }
    }
}
