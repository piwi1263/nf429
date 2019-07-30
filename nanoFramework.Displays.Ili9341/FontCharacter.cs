namespace nanoFramework.Displays.Ili9341
{
    public struct FontCharacter
    {
        private byte[] _data;
        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }

        private byte _write;
        public byte Width
        {
            get { return _write; }
            set { _write = value; }
        }

        private byte _height;
        public byte Height
        {
            get { return _height; }
            set { _height = value; }
        }

        private byte _space;
        public byte Space 
        {
            get { return _space; }
            set { _space = value; }
        }
    }
}
