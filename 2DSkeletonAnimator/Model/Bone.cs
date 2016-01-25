using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.CommandWpf;
using GongSolutions.Wpf.DragDrop;
using _2DSkeletonAnimator.Annotations;
using System.Collections.Specialized;
using System.Linq;
using _2DSkeletonAnimator.Helpers;
using _2DSkeletonAnimator.ViewModel;

namespace _2DSkeletonAnimator.Model
{
    [XmlInclude(typeof(Bone))]
    [Serializable]
    [XmlRoot("Bones")]
    public class Bone : INotifyPropertyChanged, IDeserializationCallback, IDropTarget
    {
        #region VariableDeclaration
        private double _centerPointX;
        private double _centerPointY;
        private double _endPointX;
        private double _endPointY;
        private double _posX;
        private double _posY;
        private Point _lsidePoint;
        private Point _rsidePoint;
        private double _rotation;
        private string _name;
        private int _zindex;
        private bool _selected;
        private double _scaleX;
        private double _scaleY;
        private double _boneWidth = 10;
        private bool _isMouseOver;
        public static int Count;
        private bool _isChild = false;
        private Bone _parent = null;


        private ObservableCollection<object> _childCollection;

        [NonSerialized]
        private Polygon _boneShape;

        public ObservableCollection<object> ChildCollection
        {
            get { return _childCollection; }
            set
            {
                _childCollection = value;
                OnPropertyChanged("ChildCollection");
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
                //else
                //    MessageBox.Show("Can't Set Parent if Bone is not a child!");
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

        public bool IsMouseOver
        {
            get { return _isMouseOver; }
            set
            {
                _isMouseOver = value;
                OnPropertyChanged("IsMouseOver");
            }
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (_selected)
                {
                    BoneShape.Fill = new SolidColorBrush
                    {
                        Color = Colors.DodgerBlue,
                        Opacity = 0.5f
                    };
                }
                else
                {
                    BoneShape.Fill = new SolidColorBrush
                    {
                        Color = Colors.SlateGray,
                        Opacity = 0.5f
                    };
                }
                OnPropertyChanged("Selected");
            }
        }

        public double PosX
        {
            get { return _posX; }
            set
            {
                UpdateChildPositions(value-_posX,0);
                _posX = value;
                OnPropertyChanged("PosX");
            }
        }
        public double PosY
        {
            get { return _posY; }
            set
            {
                UpdateChildPositions(0, value-_posY);
                _posY = value;
                OnPropertyChanged("PosY");
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

        public double CenterPointX
        {
            get { return _centerPointX; }
            set
            {
                if (value.Equals(_centerPointX)) return;
                _centerPointX = value;
                OnPropertyChanged("CenterPointX");
            }
        }

        public double CenterPointY
        {
            get { return _centerPointY; }
            set
            {
                if (value.Equals(_centerPointY)) return;
                _centerPointY = value;
                OnPropertyChanged("CenterPointY");
            }
        }

        public double EndPointX
        {
            get { return _endPointX; }
            set
            {
                if (value.Equals(_endPointX)) return;
                _endPointX = value;
                OnPropertyChanged("EndPointX");
            }
        }

        public double EndPointY
        {
            get { return _endPointY; }
            set
            {
                if (value.Equals(_endPointY)) return;
                _endPointY = value;
                OnPropertyChanged("EndPointY");
            }
        }

        public Polygon BoneShape
        {
            get { return _boneShape; }
            set
            {
                if (value.Equals(_boneShape)) return;
                _boneShape = value;
                OnPropertyChanged("BoneShape");
            }
        }

        public double Rotation
        {
            get { return _rotation; }
            set
            {
                if (value >= 360)
                    value %= 360;

                UpdateRotation(value - _rotation);
                _rotation = value;
                OnPropertyChanged("Rotation");


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

        public double ScaleX
        {
            get { return _scaleX; }
            set
            {
                if (value.Equals(_scaleX)) return;
                _scaleX = value;
                //UpdateScale();
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
                //UpdateScale();
                OnPropertyChanged("ScaleY");
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

        public Bone()
        {
            Name = $"bone_{Count}";
            Count++;
            ScaleX = 1;
            ScaleY = 1;
            Zindex = int.MaxValue;
            BoneShape = new Polygon
            {
                Stroke = new SolidColorBrush() { Color = Colors.Black },
                StrokeThickness = 2
            };
            Selected = true;

            ChildCollection = new ObservableCollection<object>();
            ChildCollection.CollectionChanged += OnChildCollectionChanged;
        }

        private void OnChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is Texture2D)
                    {
                        ((Texture2D)newItem).IsChild = true;
                        ((Texture2D) newItem).Parent = this;
                    }
                    else
                    {
                        ((Bone) newItem).IsChild = true;
                        ((Bone) newItem).Parent = this;
                    }
                    if (!MainViewModel.ViewportCollection.Contains(newItem) && MainViewModel.ViewportCollection.Contains(this))
                    {
                        MainViewModel.ViewportCollection.Add(newItem);
                    } 
                }
            }
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is Texture2D) (oldItem as Texture2D).IsChild = false;
                    else
                    {
                        var oldBone = oldItem as Bone;
                        (oldBone).IsChild = false;
                        if (oldBone.ChildCollection.Any())
                            oldBone.ChildCollection.RemoveAll(item => item !=null);
                    }
                    MainViewModel.ViewportCollection.Remove(oldItem);
                }
            }
        }

        public Bone(Bone bone)
        {
            if (bone != null)
            {
                CenterPointX = bone.CenterPointX;
                CenterPointY = bone.CenterPointY;
                EndPointX = bone.EndPointX;
                EndPointY = bone.EndPointY;
                PosX = bone.PosX;
                PosY = bone.PosY;
                Name = bone.Name;
                Zindex = bone.Zindex;
                BoneShape = new Polygon
                {
                    Stroke = new SolidColorBrush() {Color = Colors.Black},
                    StrokeThickness = 2
                };
                Selected = bone.Selected;
                ScaleX = bone.ScaleX;
                ScaleY = bone.ScaleY;
                IsMouseOver = bone.IsMouseOver;
                ChildCollection = new ObservableCollection<object>();
                ChildCollection.CollectionChanged += OnChildCollectionChanged;
                foreach (var child in bone.ChildCollection)
                {
                    if (child is Texture2D)
                        ChildCollection.Add(new Texture2D(child as Texture2D));
                    else
                        ChildCollection.Add(new Bone(child as Bone));
                }
                if (bone.BoneShape != null) BoneShape.Points = bone.BoneShape.Points;
                Rotation = bone.Rotation;
            }
            else
            {
                //MessageBox.Show("An error occured!");
            }
        }

        public Bone(double centerPointX, double centerPointY, double endPointX, double endPointY, string name, double scaleX, double scaleY)
        {
            CenterPointX = centerPointX;
            CenterPointY = centerPointY;
            EndPointX = endPointX;
            EndPointY = endPointY;
            if(!string.IsNullOrEmpty(name))Name = $"bone_{name}_{Count}";
            else Name = $"bone_{Count}";
            Count++;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Zindex = int.MaxValue;

            BoneShape = new Polygon
            {
                Stroke = new SolidColorBrush() {Color = Colors.Black},
                StrokeThickness = 2
            };
            Selected = true;


            Vector vec = Vector.Subtract(new Vector(EndPointX, EndPointY), new Vector(CenterPointX, CenterPointY));
            var v2 = Vector.Add(vec, new Vector(-vec.Y, vec.X));
            var v3 = Vector.Add(vec, new Vector(vec.Y, -vec.X));
            v2.Normalize();
            v3.Normalize();
            v2 = Vector.Add(Vector.Multiply(v2, _boneWidth), new Vector(CenterPointX, CenterPointY));
            v3 = Vector.Add(Vector.Multiply(v3, _boneWidth), new Vector(CenterPointX, CenterPointY));
            _lsidePoint = new Point(v2.X, v2.Y);
            _rsidePoint = new Point(v3.X, v3.Y);

            BoneShape.Points = new PointCollection { new Point(CenterPointX, CenterPointY), _lsidePoint, new Point(EndPointX, EndPointY), _rsidePoint };


            //Calculate Rotation
            var pointTop = new Vector(CenterPointX + vec.Length, CenterPointY);
            var vec2 = Vector.Subtract(pointTop, new Vector(CenterPointX, CenterPointY));
            var angle = Vector.AngleBetween(vec2, vec);
            angle += 90;
            if (angle < 0)
                angle += 360;

            _rotation = angle;

            ChildCollection = new ObservableCollection<object>();
            ChildCollection.CollectionChanged += OnChildCollectionChanged;
        }

        public void UpdateBone(double endPointX,double endPointY)
        {
            EndPointX = endPointX;
            EndPointY = endPointY;
            Selected = true;

            Vector vec = Vector.Subtract(new Vector(EndPointX, EndPointY), new Vector(CenterPointX, CenterPointY));
            var v2 = Vector.Add(vec, new Vector(-vec.Y, vec.X));
            var v3 = Vector.Add(vec, new Vector(vec.Y, -vec.X));
            v2.Normalize();
            v3.Normalize();
            v2 = Vector.Add(Vector.Multiply(v2, _boneWidth), new Vector(CenterPointX, CenterPointY));
            v3 = Vector.Add(Vector.Multiply(v3, _boneWidth), new Vector(CenterPointX, CenterPointY));
            _lsidePoint = new Point(v2.X, v2.Y);
            _rsidePoint = new Point(v3.X, v3.Y);

            BoneShape.Points = new PointCollection { new Point(CenterPointX, CenterPointY), _lsidePoint, new Point(EndPointX, EndPointY), _rsidePoint };

            var pointTop = new Vector(CenterPointX + vec.Length, CenterPointY);
            var vec2 = Vector.Subtract(pointTop, new Vector(CenterPointX, CenterPointY));
            var angle = Vector.AngleBetween(vec2, vec);
            angle += 90;
            if(angle <0)
            angle += 360;

            _rotation = angle;

        }
        public void OnDeserialization(object sender)
        {
            //Name = $"bone_{Count}";
            ScaleX = 1;
            ScaleY = 1;
            Zindex = int.MaxValue;
            BoneShape = new Polygon
            {
                Stroke = new SolidColorBrush() { Color = Colors.Black },
                StrokeThickness = 2
            };
            Selected = true;
            BoneShape.Points = new PointCollection { new Point(CenterPointX, CenterPointY), _lsidePoint, new Point(EndPointX, EndPointY), _rsidePoint };
        }
        private void UpdateRotation(double diffRotation)
        {
            var centerPoint = new Point(CenterPointX, CenterPointY);
            var endpoint = RotatePoint(new Point(EndPointX, EndPointY), centerPoint, diffRotation);
            EndPointX = endpoint.X;
            EndPointY = endpoint.Y;
            _lsidePoint = RotatePoint(_lsidePoint, centerPoint, diffRotation);
            _rsidePoint = RotatePoint(_rsidePoint, centerPoint, diffRotation);
            BoneShape.Points = new PointCollection { centerPoint, _lsidePoint, endpoint, _rsidePoint };

            if (ChildCollection != null && ChildCollection.Any())
            {
                centerPoint = new Point(PosX+ CenterPointX, PosY + CenterPointY);
                foreach (var child in ChildCollection)
                {
                    if (child is Bone)
                    {
                        
                        (child as Bone).UpdateChildRotation(centerPoint,diffRotation);
                    }
                    else if (child is Texture2D)
                    {
                        var tex = child as Texture2D;
                        var newPos = RotatePoint(new Point(tex.PosX+ tex.CenterPointX, tex.PosY + tex.CenterPointY), centerPoint, diffRotation);
                        tex.PosX = newPos.X - tex.CenterPointX;
                        tex.PosY = newPos.Y - tex.CenterPointY;
                        tex.Rotation += diffRotation;
                    }
                }
            }
        }

        private void UpdateChildRotation(Point center, double diffRotation)
        {
            var beginPos = RotatePoint(new Point(PosX+ CenterPointX, PosY + CenterPointY), center, diffRotation);
            var endPos = RotatePoint(new Point(PosX + EndPointX, PosY + EndPointY), center, diffRotation);
            CenterPointX = beginPos.X - PosX;
            CenterPointY = beginPos.Y - PosY;

            UpdateBone(endPos.X - PosX, endPos.Y - PosY);
            if (ChildCollection != null)
            {
                foreach (var child in ChildCollection)
                {
                    if (child is Bone)
                    {
                        (child as Bone).UpdateChildRotation(center, diffRotation);
                    }
                    else if (child is Texture2D)
                    {
                        var tex = child as Texture2D;
                        var newPos = RotatePoint(new Point(tex.PosX + tex.CenterPointX, tex.PosY + tex.CenterPointY), center, diffRotation);
                        tex.PosX = newPos.X - tex.CenterPointX;
                        tex.PosY = newPos.Y - tex.CenterPointY;
                        tex.Rotation += diffRotation;
                    }
                }
            }
        }

        public void UpdateChildPositions(double diffX,double diffY)
        {
            if (IsChild)
            {
                _posX += diffX;
                _posY += diffY;
                OnPropertyChanged("PosX");
                OnPropertyChanged("PosY");
            }
            if (ChildCollection != null && ChildCollection.Any())
            {
                foreach (var child in ChildCollection)
                {
                    if (child is Bone)
                    {
                        (child as Bone).UpdateChildPositions(diffX, diffY);
                    }
                    else if (child is Texture2D)
                    {
                        var tex = child as Texture2D;
                        tex.PosX += diffX;
                        tex.PosY += diffY;
                    }
                }
            }
        }

        private void UpdateScale()
        {
            Vector vec = Vector.Subtract(new Vector(EndPointX, EndPointY), new Vector(CenterPointX, CenterPointY));
            var v2 = Vector.Add(vec, new Vector(-vec.Y, vec.X));
            var v3 = Vector.Add(vec, new Vector(vec.Y, -vec.X));
            v2.Normalize();
            v3.Normalize();
            v2 = Vector.Add(Vector.Multiply(v2, _boneWidth * ScaleX), new Vector(CenterPointX, CenterPointY));
            v3 = Vector.Add(Vector.Multiply(v3, _boneWidth * ScaleX), new Vector(CenterPointX, CenterPointY));
            _lsidePoint = new Point(v2.X, v2.Y);
            _rsidePoint = new Point(v3.X, v3.Y);

            BoneShape.Points = new PointCollection { new Point(CenterPointX, CenterPointY), _lsidePoint, new Point(EndPointX, EndPointY), _rsidePoint };
        }


        static Point RotatePoint(Point pointToRotate, Point centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Point
            {
                X = (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        public void MouseEnter()
        {
            IsMouseOver = true;
        }
        public void MouseLeave()
        {
            IsMouseOver = false;
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
            if (dropInfo.Data is Bone || dropInfo.Data is Texture2D)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            var tex = dropInfo.Data as Texture2D;
            var bone = dropInfo.Data as Bone;
            var texArr = dropInfo.Data as List<Texture2D>;
            var boneArr = dropInfo.Data as List<Bone>;
            if (tex != null && !ChildCollection.Contains(tex))
            {
                MainViewModel.ViewportCollection.Remove(tex);
                if (!ChildCollection.Contains(tex))
                    ChildCollection.Add(tex);
            }
            else if (texArr != null)
            {
                foreach (var t in texArr)
                {
                        MainViewModel.ViewportCollection.Remove(t);
                    if (!ChildCollection.Contains(t))
                        ChildCollection.Add(t);
                }
            }
            else if (bone != null && !ChildCollection.Contains(bone) && bone != this)
            {
                MainViewModel.ViewportCollection.Remove(bone);
                if(!ChildCollection.Contains(bone))
                    ChildCollection.Add(bone);
            }
                
            else if (boneArr != null)
            {
                foreach (var b in boneArr)
                {
                    if (b != null && !ChildCollection.Contains(b) && b != this)
                    {
                        MainViewModel.ViewportCollection.Remove(b);
                        if (!ChildCollection.Contains(b))
                            ChildCollection.Add(b);
                    }
                }
            }
        }
    }
}