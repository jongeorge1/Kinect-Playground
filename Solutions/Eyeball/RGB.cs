namespace Eyeball
{
    using System;

    public class Rgb
    {
        private byte? redByte;
        private byte? greenByte;
        private byte? blueByte;

        public byte? Green {
            get { return this.greenByte; }
            set { this.greenByte = value; }
        }

        public byte? Blue {
            get { return this.blueByte; }
            set { this.blueByte = value; }
        }

        public byte? Red {
            get { return this.redByte; }
            set { this.redByte = value; }
        }
        public Rgb() { }
        public Rgb(String Raw) {
            string[] val = Raw.Split(';');
            this.redByte = Convert.ToByte(val[0]);
            this.greenByte = Convert.ToByte(val[1]);
            this.blueByte = Convert.ToByte(val[2]);
        }

        public override string ToString() {
            return (string.Format("{0},{1},{2}",this.redByte,this.greenByte,this.blueByte));
        }
    }
}
