using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.CommandWpf;
using GongSolutions.Wpf.DragDrop;
using _2DSkeletonAnimator.Annotations;

namespace _2DSkeletonAnimator.Model
{
    [XmlInclude(typeof(Texture2D))]
    [Serializable]
    [XmlRoot("Textures")]
    public class Texture2D : INotifyPropertyChanged, IDeserializationCallback, IDropTarget
    {
        #region VariableDeclaration
        private string _name;
        private string _path;
        private double _rotation;
        private double _scaleX;
        private double _scaleY;
        private float _width;
        private float _height;
        private int _zindex;
        private bool _selected;
        private int _id;
        private double _posX;
        private double _posY;
        private double _centerPointX;
        private double _centerPointY;
        private Bone _texBone;
        private bool _isMouseOver;
        private bool _isChild = false;
        private Bone _parent = null;
        [NonSerialized]
        private Image _texture;


        public bool IsMouseOver
        {
            get { return _isMouseOver; }
            set
            {
                if (value == _isMouseOver) return;
                _isMouseOver = value;
                OnPropertyChanged("IsMouseOver");
            }
        }

        public Bone Parent
        {
            get { return _parent; }
            set
            {
                if (IsChild)
                {
                    _parent = value;
                    OnPropertyChanged("Parent");
                }
                else
                    MessageBox.Show("Can't Set Parent if Bone is not a child!");
            }
        }

        public bool IsChild
        {
            get { return _isChild; }
            set
            {
                if (value == _isChild) return;
                _isChild = value;
                OnPropertyChanged("IsChild");
            }
        }

        public Image Texture
        {
            get { return _texture; }
            set
            {
                if (Equals(value, _texture)) return;
                _texture = value;
                _path = _texture.Source.ToString();
                OnPropertyChanged("Texture");
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public double CenterPointX
        {
            get { return _centerPointX; }
            private set
            {
                _centerPointX = value;
                OnPropertyChanged("CenterPointX");
            }
        }
        public double CenterPointY
        {
            get { return _centerPointY; }
            private set
            {
                _centerPointY = value;
                OnPropertyChanged("CenterPointY");
            }
        }

        public double Rotation
        {
            get { return _rotation; }
            set
            {
                if (value.Equals(_rotation)) return;
                if (value >= 360) value %= 360;
                else if (value < 0) value += 360;
                _rotation = value;
                OnPropertyChanged("Rotation");
            }
        }


        public double ScaleX
        {
            get { return _scaleX; }
            set
            {
                if (value.Equals(_scaleX)) return;
                _scaleX = value;
                CenterPointX = Width / 2 * ScaleX;
                OnPropertyChanged("ScaleX");
            }
        }

        public double ScaleY
        {
            get { return _scaleY; }
            set
            {
                if (value.Equals(_scaleY)) return;
                _scaleY = value;
                CenterPointY = Height / 2 * ScaleY;
                OnPropertyChanged("ScaleY");
            }
        }

        public float Width
        {
            get { return _width; }
            set
            {
                if (value.Equals(_width)) return;
                _width = value;
                CenterPointX = Width / 2 * ScaleX;
                OnPropertyChanged("Width");
            }
        }

        public float Height
        {
            get { return _height; }
            set
            {
                if (value.Equals(_height)) return;
                _height = value;
                CenterPointY = Height / 2 * ScaleY;
                OnPropertyChanged("Height");
            }
        }

        public int Zindex
        {
            get { return _zindex; }
            set
            {
                if (value == _zindex) return;
                _zindex = value;
                OnPropertyChanged("Zindex");
            }
        }

        public double PosX
        {
            get { return _posX; }
            set
            {
                _posX = value;
                OnPropertyChanged("PosX");
            }
        }
        public double PosY
        {
            get { return _posY; }
            set
            {
                _posY = value;
                OnPropertyChanged("PosY");
            }
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (value == _selected) return;
                _selected = value;
                OnPropertyChanged("Selected");
            }
        }

        public int Id
        {
            get { return _id; }
            set
            {
                if (value == _id) return;
                _id = value;
                OnPropertyChanged("Id");
            }
        }

        public Bone TexBone
        {
            get { return _texBone; }
            set
            {
                if (Equals(value, _texBone)) return;
                _texBone = value;
                OnPropertyChanged("TexBone");
            }
        }
        #endregion

        #region Commands
        [NonSerialized]
        private RelayCommand _mouseEnterCommand;
        [NonSerialized]
        private RelayCommand _mouseLeaveCommand;
        public RelayCommand MouseEnterCommand
        {
            get { return _mouseEnterCommand ?? (_mouseEnterCommand = new RelayCommand(MouseEnter)); }
        }
        public RelayCommand MouseLeaveCommand
        {
            get { return _mouseLeaveCommand ?? (_mouseLeaveCommand = new RelayCommand(MouseLeave)); }
        }
        #endregion

        public Texture2D()
        {
            var rand = new Random();
            Id = rand.Next(1000000000);
            ScaleX = 1;
            ScaleY = 1;
        }

        public Texture2D(Texture2D tex)
        {
            if (tex != null)
            {
                Texture = tex.Texture;
                Name = tex.Name;
                Rotation = tex.Rotation;
                ScaleX = tex.ScaleX;
                ScaleY = tex.ScaleY;
                Width = tex.Width;
                Height = tex.Height;
                Zindex = tex.Zindex;
                PosX = tex.PosX;
                PosY = tex.PosY;
                Selected = tex.Selected;

                var rand = new Random();
                Id = rand.Next(1000000000);
            }
            else
            {
                //TODO HANDLE THIS BETTER
                MessageBox.Show("An error occured!");
            }

        }

        public Texture2D(Image texture, string name, double rotation, float width, float height, int zindex, double posX,double posY, bool selected,double scaleX = 1.0, double scaleY = 1.0)
        {
            if(texture !=null) Texture = texture;
            Name = name;
            Rotation = rotation;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Width = width;
            Height = height;
            Zindex = zindex;
            PosX = posX;
            PosY = posY;
            Selected = selected;

            var rand = new Random();
            _id = rand.Next(1000000000);
        }

        public void MouseEnter()
        {
            IsMouseOver = true;
        }
        public void MouseLeave()
        {
            IsMouseOver = false;
        }

        public void OnDeserialization(object sender)
        {
            try
            {
                var imgSource = new BitmapImage(new Uri(_path, UriKind.RelativeOrAbsolute));
                Texture = new Image { Source = imgSource };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to load image!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
 
        }
        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            Texture2D tex = obj as Texture2D;
            return tex?.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void DragOver(IDropInfo dropInfo)
        {
            dropInfo.Effects = DragDropEffects.None;
        }

        public void Drop(IDropInfo dropInfo)
        {
            throw new NotImplementedException();
        }
    }
}