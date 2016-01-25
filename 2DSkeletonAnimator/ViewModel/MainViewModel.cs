using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.CommandWpf;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using ZoomWpfTest;
using _2DSkeletonAnimator.Helpers;
using _2DSkeletonAnimator.Model;
using UnRedoPair = System.Collections.Generic.KeyValuePair<System.Action,System.Action>;  

namespace _2DSkeletonAnimator.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged, IDropTarget
    {
        #region VariablesDeclaration
        public ObservableCollection<Object> TextureCollection { get; set; }
        public static ObservableCollection<Object> ViewportCollection { get; set; }

        public ObservableCollection<Object> ViewportListCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private Texture2D _scaledTexture, _dragAndDroppedTexture;
        private object _draggedObject;
        private object _rotatedObject;

        private Cursor _cursor;
        private int _scaleOffsetArea;
        private Point _mousePosition;
        private bool _isAnimationMode, _isDrawBonesMode;
        private double _windowWidth = 960, _windowHeight = 480;
        private object _selectedItem;
        private Bone _boneDrawn = null;
        public bool _isItemSelected = false;
        private string _projectName;

        //FOR UNDO
        private Stack<UnRedoPair> _undoStack;
        private Stack<UnRedoPair> _redoStack;
        private bool _clearRedoStack = true;
        private object _objPreTranslated;
        private object _objPreTranslatedGrid;

        public string ProjectName
        {
            get { return _projectName; }
            set { _projectName = value; }
        }

        public bool IsAnimationMode
        {
            get { return _isAnimationMode; }
            set
            {
                _isAnimationMode = value;
                OnPropertyChanged("IsAnimationMode");
            }
        }

        public bool IsDrawBonesMode
        {
            get { return _isDrawBonesMode; }
            set
            {
                _isDrawBonesMode = value;
                OnPropertyChanged("IsDrawBonesMode");
            }
        }

        public bool IsItemSelected
        {
            get { return _isItemSelected; }
            set
            {
                _isItemSelected = value;
                OnPropertyChanged("IsItemSelected");
            }
        }

        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        public virtual double WindowWidth
        {
            get { return _windowWidth; }
            set
            {
                _windowWidth = value;
                OnPropertyChanged("WindowWidth");
            }
        }

        public virtual double WindowHeight
        {
            get { return _windowHeight; }
            set
            {
                _windowHeight = value;
                OnPropertyChanged("WindowHeight");
            }
        }

        public int ScaleOffsetArea
        {
            get { return _scaleOffsetArea; }
            set
            {
                _scaleOffsetArea = value;
                OnPropertyChanged("ScaleOffsetArea");
            }
        }

        public Point MousePosition
        {
            get { return _mousePosition; }
            set
            {
                _mousePosition = value;
                OnPropertyChanged("MousePosition");
            }
        }
        public Cursor Cursor
        {
            get { return _cursor; }
            set
            {
                _cursor = value;
                OnPropertyChanged("Cursor");
            }
        }

        public Object DraggedObject
        {
            get { return _draggedObject; }
            set
            {
                _draggedObject = value;
                OnPropertyChanged("DraggedObject");
            }
        }

        public Texture2D ScaledTexture
        {
            get { return _scaledTexture; }
            set
            {
                _scaledTexture = value;
                OnPropertyChanged("ScaledTexture");
            }
        }

        public object RotatedObject
        {
            get { return _rotatedObject; }
            set
            {
                _rotatedObject = value;
                OnPropertyChanged("RotatedTexture");
            }
        }

        public Texture2D DragAndDroppedTexture
        {
            get { return _dragAndDroppedTexture; }
            set
            {
                _dragAndDroppedTexture = value;
                OnPropertyChanged("DragAndDroppedTexture");
            }
        }

        #endregion
        //Commands
        #region Commands
        private RelayCommand _importTextureCommand;
        private RelayCommand _openProjectCommand;
        private RelayCommand _saveProjectCommand;
        private RelayCommand<PanAndZoomViewer> _closeProjectCommand;
        private RelayCommand _toggleDrawBonesCommand;
        private RelayCommand _inputGridCommand;
        private RelayCommand _potentialInputGridCommand;
        private RelayCommand _undoCommand;
        private RelayCommand _redoCommand;
        private RelayCommand<object> _lboxLeftClickCommand;
        private RelayCommand<object> _lboxRightClickDeleteCommand;
        private RelayCommand<object> _lboxRightClickResetCommand;
        private RelayCommand<object> _lboxRightClickAddBoneCommand;
        private RelayCommand<MouseEventArgs> _canvasMouseBtnLDownCommand;
        private RelayCommand<MouseEventArgs> _canvasMouseBtnRDownCommand;
        private RelayCommand<MouseEventArgs> _canvasMouseBtnLUpCommand;
        private RelayCommand<MouseEventArgs> _canvasMouseMoveCommand;


        public RelayCommand ImportTextureCommand
        {
            get { return _importTextureCommand ?? (_importTextureCommand = new RelayCommand(ImportTexture)); }
        }
        public RelayCommand OpenProjectCommand
        {
            get { return _openProjectCommand ?? (_openProjectCommand = new RelayCommand(OpenProject)); }
        }
        public RelayCommand SaveProjectCommand
        {
            get { return _saveProjectCommand ?? (_saveProjectCommand = new RelayCommand(SaveProject)); }
        }
        public RelayCommand<PanAndZoomViewer> CloseProjectCommand
        {
            get { return _closeProjectCommand ?? (_closeProjectCommand = new RelayCommand<PanAndZoomViewer>(CloseProject)); }
        }
        public RelayCommand ToggleDrawBonesCommand
        {
            get { return _toggleDrawBonesCommand ?? (_toggleDrawBonesCommand = new RelayCommand(ToggleDrawBones)); }
        }
        public RelayCommand InputGridCommand
        {
            get { return _inputGridCommand ?? (_inputGridCommand = new RelayCommand(InputGrid)); }
        }
        public RelayCommand PotentialInputGridCommand
        {
            get { return _potentialInputGridCommand ?? (_potentialInputGridCommand = new RelayCommand(PotentialInputGrid)); }
        }
        public RelayCommand<object> LboxLeftClickCommand
        {
            get { return _lboxLeftClickCommand ?? (_lboxLeftClickCommand = new RelayCommand<object>(LBoxLeftClick)); }
        }
        public RelayCommand<object> LboxRightClickDeleteCommand
        {
            get { return _lboxRightClickDeleteCommand ?? (_lboxRightClickDeleteCommand = new RelayCommand<object>(LBoxRightClickDelete)); }
        }
        public RelayCommand<object> LboxRightClickResetCommand
        {
            get { return _lboxRightClickResetCommand ?? (_lboxRightClickResetCommand = new RelayCommand<object>(LBoxRightClickReset)); }
        }

        public RelayCommand<object> LboxRightClickAddBoneCommand
        {
            get { return _lboxRightClickAddBoneCommand ?? (_lboxRightClickAddBoneCommand = new RelayCommand<object>(LBoxRightClickAddBone)); }
        }

        public RelayCommand<MouseEventArgs> CanvasMouseBtnLDownCommand
        {
            get { return _canvasMouseBtnLDownCommand ?? (_canvasMouseBtnLDownCommand = new RelayCommand<MouseEventArgs>(CanvasMouseBtnLDown)); }
        }
        public RelayCommand<MouseEventArgs> CanvasMouseBtnRDownCommand
        {
            get { return _canvasMouseBtnRDownCommand ?? (_canvasMouseBtnRDownCommand = new RelayCommand<MouseEventArgs>(CanvasMouseBtnRDown)); }
        }
        public RelayCommand<MouseEventArgs> CanvasMouseBtnLUpCommand
        {
            get { return _canvasMouseBtnLUpCommand ?? (_canvasMouseBtnLUpCommand = new RelayCommand<MouseEventArgs>(CanvasMouseBtnLUp)); }
        }
        public RelayCommand<MouseEventArgs> CanvasMouseMoveCommand
        {
            get { return _canvasMouseMoveCommand ?? (_canvasMouseMoveCommand = new RelayCommand<MouseEventArgs>(CanvasMouseMove)); }
        }

        public RelayCommand UndoCommand
        {
            get { return _undoCommand ?? (_undoCommand = new RelayCommand(Undo)); }
        }

        public RelayCommand RedoCommand
        {
            get { return _redoCommand ?? (_redoCommand = new RelayCommand(Redo)); }
        }

        #endregion

        public MainViewModel()
        {
            TextureCollection = new ObservableCollection<object>();
            ViewportCollection = new ObservableCollection<object>();
            ViewportCollection.CollectionChanged += OnViewportCollectionChanged;
            ViewportListCollection = new ObservableCollection<object>();
            _undoStack = new Stack<UnRedoPair>(); //Key = Undo action Value = Redo Action
            _redoStack = new Stack<UnRedoPair>(); //Key = Redo action Value = Undo Action
            _scaleOffsetArea = 10;
        }
        private void OnViewportCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if((newItem is Texture2D && (newItem as Texture2D).IsChild == false) ||
                        (newItem is Bone && (newItem as Bone).IsChild == false))
                        if(!ViewportListCollection.Contains(newItem)) ViewportListCollection.Add(newItem);
                }
            }
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    ViewportListCollection.Remove(oldItem);
                }
            }
        }

        public void ToggleDrawBones()
        {
            IsDrawBonesMode = !IsDrawBonesMode;
        }

        public void Undo()
        {
            if (_undoStack.Any())
            {
                var undoAction = _undoStack.Pop();
                var redoAction = new UnRedoPair(undoAction.Value, undoAction.Key); //Redo is Undo swapped
                _redoStack.Push(redoAction);
                undoAction.Key.Invoke();

                UpdateSelectedItem();
            }
        }

        public void Redo()
        {
            if (_redoStack.Any())
            {
                var redoAction = _redoStack.Pop();
                redoAction.Key.Invoke(); //Redo action is reverse added to undostack again

                UpdateSelectedItem();
            }
        }

        public void AddUndoAction(UnRedoPair pair)
        {
            _undoStack.Push(pair);
            if(_clearRedoStack) _redoStack.Clear();
            _clearRedoStack = true;
        }

        public void PotentialInputGrid()
        {
            if(SelectedItem is Texture2D)
                _objPreTranslatedGrid = new Texture2D((Texture2D) SelectedItem);
            else if (SelectedItem is Bone)
                _objPreTranslatedGrid = new Bone((Bone) SelectedItem);
        }

        public void InputGrid()
        {
            var undoObj = SelectedItem; //Make Ref of SelectedItem for undo
            if (_objPreTranslatedGrid is Texture2D)
            {
                var sTex = new Texture2D(SelectedItem as Texture2D);
                var sOrigTex = new Texture2D(_objPreTranslatedGrid as Texture2D);

                if (sTex.PosX != sOrigTex.PosX || sTex.PosY != sOrigTex.PosY || sTex.ScaleX != sOrigTex.ScaleX ||
                    sTex.ScaleY != sOrigTex.ScaleY || sTex.Rotation != sOrigTex.Rotation)
                {
                    AddUndoAction(new UnRedoPair(() =>
                    { Translate(undoObj, sOrigTex.PosX, sOrigTex.PosY, sOrigTex.ScaleX, sOrigTex.ScaleY, sOrigTex.Rotation, true); },
                    () => { Translate(undoObj, sTex.PosX, sTex.PosY, sTex.ScaleX, sTex.ScaleY, sTex.Rotation, false); }));
                }
            }
            else if (_objPreTranslatedGrid is Bone)
            {
                var sBone = new Bone(SelectedItem as Bone);
                var sOrigBone = new Bone(_objPreTranslatedGrid as Bone);

                if (sBone.PosX != sOrigBone.PosX || sBone.PosY != sOrigBone.PosY || sBone.ScaleX != sOrigBone.ScaleX ||
                    sBone.ScaleY != sOrigBone.ScaleY || sBone.Rotation != sOrigBone.Rotation)
                {
                    AddUndoAction(new UnRedoPair(() =>
                    { Translate(undoObj, sOrigBone.PosX, sOrigBone.PosY, sOrigBone.ScaleX, sOrigBone.ScaleY, sOrigBone.Rotation, true); },
                    () => { Translate(undoObj, sBone.PosX, sBone.PosY, sBone.ScaleX, sBone.ScaleY, sBone.Rotation, false); }));
                }
            }
            _objPreTranslatedGrid = null;
        }

        
        private void UpdateSelectedItem()
        {
            SelectedItem = null;
            int selectCount = 0;
            foreach (var obj in ViewportCollection)
            {
                if (obj is Texture2D && (obj as Texture2D).Selected)
                {
                    SelectedItem = obj;
                    selectCount++;
                }
                else if(obj is Bone && (obj as Bone).Selected)
                {
                    SelectedItem = obj;
                    selectCount++;
                }
                if (selectCount > 1)
                {
                    SelectedItem = null;
                    break;
                }
            }
        }

        //TODO IMPLEMENT COMMANDS RIGHT CLICK AND MOVE

        private void ImportTexture()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (.jpg, .jpeg, .gif, .bmp, .png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                int i = 0;
                foreach (var fName in dialog.FileNames)
                {
                    var bitmap = new BitmapImage(new Uri(fName, UriKind.RelativeOrAbsolute));
                    var image = new Image { Source = bitmap };
                    var name = string.IsNullOrEmpty(dialog.SafeFileNames[i]) ? dialog.FileName : dialog.SafeFileNames[i];

                    Texture2D tex = new Texture2D
                    {
                        Texture = image,
                        Name = name,
                        Height = (float)image.Source.Height,
                        Width = (float)image.Source.Width,
                        PosX = 0,
                        PosY = 0,
                        ScaleX = 1.0,
                        ScaleY = 1.0,
                        Selected = false
                    };

                    TextureCollection.Add(tex);
                    i++;
                }
            }
        }

        private void OpenProject()
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = ".2dsk";
            if (dlg.ShowDialog() == true)
            {
                ProjectName = Path.GetFileNameWithoutExtension(dlg.FileName);
                CloseProject(null);
                var binFormatter = new BinaryFormatter();
                try
                {
                    using (Stream stream = File.Open(dlg.FileName, FileMode.Open))
                    {
                        var bformatter = new BinaryFormatter();
                        //List<object> viewportListCollectionList = (List<object>)bformatter.Deserialize(stream);
                        //foreach (var obj in viewportListCollectionList)
                        //    ViewportListCollection.Add(obj);
                        List<object> viewportList = (List<object>)bformatter.Deserialize(stream);
                        foreach (var obj in viewportList)
                            ViewportCollection.Add(obj);
                        List<object> textureList = (List<object>)bformatter.Deserialize(stream);
                        foreach (var obj in textureList)
                            TextureCollection.Add(obj);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error : Failed to open the project!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void SaveProject()
        {
            var dlg = new SaveFileDialog();
            if (!string.IsNullOrEmpty(ProjectName)) dlg.FileName = ProjectName;
            dlg.AddExtension = true;
            dlg.DefaultExt = ".2dsk";
            if (dlg.ShowDialog() == true)
            {
                ProjectName = Path.GetFileNameWithoutExtension(dlg.FileName);
                try
                {
                    using (Stream stream = File.Open(dlg.FileName, FileMode.Create))
                    {
                        var bformatter = new BinaryFormatter();
                        //bformatter.Serialize(stream,ViewportListCollection.ToList());
                        bformatter.Serialize(stream, ViewportCollection.ToList());
                        bformatter.Serialize(stream, TextureCollection.ToList());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error : Failed to save the project!", MessageBoxButton.OK, MessageBoxImage.Error);
                    var result = MessageBox.Show("Do you want to try and save again?", "Save project", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes) SaveProject();
                }
            }
        }

        private void CloseProject(PanAndZoomViewer panAndZoomViewer)
        {
            //Ask user to save
            if (panAndZoomViewer != null)
            {
                var result = MessageBox.Show("Do you want to save the current project? Clicking no will make you lose all changes !",
                                               "Save project", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) SaveProject();
            }


            SelectedItem = null;
            ProjectName =String.Empty;
            ViewportCollection.Clear();
            ViewportListCollection.Clear();
            TextureCollection.Clear();
            IsAnimationMode = false;
            IsDrawBonesMode = false;
            IsItemSelected = false;
            Bone.Count = 0; //For naming of the bones

            //Just in case
            DraggedObject = null;
            RotatedObject = null;
            ScaledTexture = null;
            DragAndDroppedTexture = null;
            Cursor = Cursors.Arrow;
            if(panAndZoomViewer != null) panAndZoomViewer.Reset();

            //UNDO
            _undoStack.Clear();
            _redoStack.Clear();
            _clearRedoStack = true;
            _objPreTranslated = null;
            _objPreTranslatedGrid = null;
            UpdateSelectedItem();


        }

        private void LBoxLeftClick(object obj)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) ||Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (obj is Texture2D) ((Texture2D) obj).Selected = true;
                else if (obj is Bone) ((Bone) obj).Selected = true;
            }
            else
            {
                foreach (var item in ViewportCollection)
                {
                    if (item is Texture2D) ((Texture2D)item).Selected = false;
                    else if (item is Bone) ((Bone)item).Selected = false;
                }
                if (obj is Texture2D) ((Texture2D)obj).Selected = true;
                else if (obj is Bone) ((Bone)obj).Selected = true;
            }
            UpdateSelectedItem();
        }

        private void LBoxRightClickDelete(object obj)
        {
            AddUndoAction(new UnRedoPair(() => RedoDrop(new List<object>() {obj}), 
                (() => { _clearRedoStack = false; LBoxRightClickDelete(obj); })));

            if (!IsAnimationMode)
            {
                foreach (var item in ViewportCollection)
                {
                    if (item == obj)
                    {
                        if (item is Bone)
                        {
                            var bone = item as Bone;
                            bone.ChildCollection.RemoveAll(b => b != null);
                            if (bone.IsChild)
                                bone.Parent.ChildCollection.Remove(bone);
                        }
                        else
                        {
                            var tex = item as Texture2D;
                            if (tex.IsChild)
                                tex.Parent.ChildCollection.Remove(tex);
                        }

                        ViewportCollection.Remove(item);
                        break;
                    }
                }
                UpdateSelectedItem();
            }
        }

        private void LBoxRightClickAddBone(object obj)
        {

            if (!IsAnimationMode)
            {
                if (obj is Texture2D)
                {
                    var tex = obj as Texture2D;

                    var bone = new Bone(tex.PosX + tex.CenterPointX, tex.PosY + tex.CenterPointY, tex.PosX, tex.PosY, $"{Path.GetFileNameWithoutExtension(tex.Name)}", tex.ScaleX, tex.ScaleY);
                    bone.Selected = false;
                    ViewportCollection.Remove(tex);
                    ViewportCollection.Add(bone);
                    bone.ChildCollection.Add(tex);


                    AddUndoAction(new UnRedoPair(() => UndoDrop(new List<object>() { bone }),
                        (() => { _clearRedoStack = false; RedoDrop(new List<object>() { bone }); })));

                }
                else if (obj is Bone)
                {
                    var bone = obj as Bone;
                    Vector oldBoneVec = Vector.Subtract(new Vector(bone.EndPointX, bone.EndPointY), new Vector(bone.CenterPointX, bone.CenterPointY));
                    oldBoneVec = Vector.Multiply(oldBoneVec, 2);
                    oldBoneVec = Vector.Add(oldBoneVec, new Vector(bone.CenterPointX, bone.CenterPointY));
                    
                    var newBone = new Bone(bone.EndPointX, bone.EndPointY, oldBoneVec.X, oldBoneVec.Y, string.Empty, bone.ScaleX, bone.ScaleY);
                    newBone.PosX = bone.PosX;
                    newBone.PosY = bone.PosY;
                    newBone.Selected = false;
                    bone.ChildCollection.Add(newBone);

                    AddUndoAction(new UnRedoPair(() => UndoDrop(new List<object>() { newBone }),
                        (() => { _clearRedoStack = false; RedoDrop(new List<object>() { newBone }); })));
                }
            }
            UpdateSelectedItem();
        }

        private void LBoxRightClickReset(object obj)
        {
            if (obj is Texture2D && !IsAnimationMode)
            {
                var tex = obj as Texture2D;
                var undoTex = new Texture2D(tex);
                tex.PosX = 0;
                tex.PosY = 0;
                tex.ScaleX = 1;
                tex.ScaleY = 1;
                tex.Rotation = 0;

                 AddUndoAction(new UnRedoPair(() =>
                 {
                     tex.PosX = undoTex.PosX;
                     tex.PosY = undoTex.PosY; 
                    tex.ScaleX = undoTex.ScaleX;
                    tex.ScaleY = undoTex.ScaleY;
                    tex.Rotation = undoTex.Rotation;
                 }, () => 
                 { _clearRedoStack = false; LBoxRightClickReset(obj); }));
            }
            else if (obj is Bone && !IsAnimationMode)
            {
                var bone = obj as Bone;
                var undoBone = new Bone(bone);
                bone.Selected = false;
                bone.Rotation = 0;
                bone.ScaleX = 1;
                bone.ScaleY = 1;

                AddUndoAction(new UnRedoPair(() =>
                {
                    bone.Selected = undoBone.Selected;
                    bone.Rotation = undoBone.Rotation;
                    bone.ScaleX = undoBone.ScaleX;
                    bone.ScaleY = undoBone.ScaleY;
                }, () => { _clearRedoStack = false; LBoxRightClickReset(obj); }));
            }
            UpdateSelectedItem();
        }

        public void CanvasMouseBtnLDown(MouseEventArgs e)
        {
            var itemsControl = e.Source as ItemsControl;
            _mousePosition = new Point(e.GetPosition(itemsControl).X, e.GetPosition(itemsControl).Y); //get mousePos
            if (itemsControl?.Items.Count > 0 && !IsDrawBonesMode && !IsAnimationMode)
            {
                //Filter out  images and bones that the cursor overlaps
                var texlist = new List<Texture2D>();
                var bonelist = new List<Bone>();
                try
                {
                    texlist = ViewportCollection.OfType<Texture2D>().ToList();
                    bonelist = ViewportCollection.OfType<Bone>().ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                

                var texOverlapList = texlist.Where(item => item.IsMouseOver).ToList();
                var boneOverlapList = bonelist.Where(item => item.IsMouseOver).ToList();
                if (texOverlapList.Count > 0 || boneOverlapList.Count > 0)
                {
                    Texture2D tex = null;
                    Bone bone = null;

                    if (texOverlapList.Count >= 1 ) tex = texOverlapList.Aggregate((i1, i2) => i1.Zindex > i2.Zindex ? i1 : i2);
                    if (boneOverlapList.Count >= 1) bone = boneOverlapList.Aggregate((i1, i2) => i1.Zindex > i2.Zindex ? i1 : i2);

                    if (bone != null)
                    {
                        //Set Everything else as not selected
                        foreach (var obj in ViewportCollection)
                        {
                            if (obj is Texture2D) ((Texture2D)obj).Selected = false;
                            else if (obj is Bone) ((Bone)obj).Selected = false;
                        }
                        Cursor = Cursors.SizeAll;
                        bone.Selected = true;
                        DraggedObject = bone;
                        _objPreTranslated = new Bone(bone);
                    }
                    else if (tex != null)
                    {
                        //check if cursor is in scale region
                        if (_mousePosition.X > (tex.PosX + tex.Width * tex.ScaleX) - _scaleOffsetArea &&
                            _mousePosition.X < (tex.PosX + tex.Width * tex.ScaleX) + _scaleOffsetArea &&
                            _mousePosition.Y > (tex.PosY + tex.Height * tex.ScaleY) - _scaleOffsetArea &&
                            _mousePosition.Y < (tex.PosY + tex.Height * tex.ScaleY) + _scaleOffsetArea)
                        {
                            ScaledTexture = tex;
                            _objPreTranslated = new Texture2D(tex);
                            Cursor = Cursors.SizeNWSE;
                        }
                        else //cursor was inside image 
                        {
                            //Set Everything else as not selected
                            foreach (var obj in itemsControl.Items)
                            {
                                if (obj is Texture2D) ((Texture2D)obj).Selected = false;
                                else if (obj is Bone) ((Bone)obj).Selected = false;
                            }
                            Cursor = Cursors.SizeAll;
                            tex.Selected = true;
                            DraggedObject = tex;
                            _objPreTranslated = new Texture2D(tex);
                        }
                    }
                }
                else //Cursor is clicking on empty canvas 
                {
                    //check if a bone or texture is selected if so it will be rotated in the mousemove
                    foreach (var texture in texlist) 
                    {
                        if (texture.Selected) 
                        {
                            RotatedObject = texture;
                            _objPreTranslated = new Texture2D(texture);
                        }
                    }
                    if (RotatedObject == null)
                    {
                        foreach (var bone in bonelist)
                        {
                            if (bone.Selected)
                            {
                                RotatedObject = bone;
                                _objPreTranslated = new Bone(bone);
                                break;
                            }
                        }
                    }
                }
            }
            else if(IsDrawBonesMode && !IsAnimationMode)
            {
                if (_boneDrawn == null)
                {
                    _boneDrawn = new Bone(_mousePosition.X,_mousePosition.Y, _mousePosition.X, _mousePosition.Y, string.Empty, 1.0,1.0) {Selected = true};
                    ViewportCollection.Add(_boneDrawn);
                }
                else
                {
                    //On second left click we are done drawing the bone
                    //Add Undo Action
                    var bone = _boneDrawn; //make a copy of the ref
                    AddUndoAction(new UnRedoPair(() => 
                    UndoDrop(new List<object>() { bone }), 
                    () => { _clearRedoStack = false; RedoDrop(new List<object>() { bone }); }));

                    _boneDrawn.Selected = false;
                    _boneDrawn = null;
                }

            }
            UpdateSelectedItem();
        }

        public void CanvasMouseBtnLUp(MouseEventArgs e)
        {
            //Undo
            if (DraggedObject != null)
            {
                if (_objPreTranslated is Texture2D)
                {
                    var draggedTex = DraggedObject as Texture2D; //make a ref to the dragged object
                    double origX = (_objPreTranslated as Texture2D).PosX;
                    double origY = (_objPreTranslated as Texture2D).PosY;
                    double newX = draggedTex.PosX;
                    double newY = draggedTex.PosY;

                    AddUndoAction(new UnRedoPair(() => Move(draggedTex,origX,origY,true), () => Move(draggedTex, newX, newY, false)));
                }
                else
                {
                    var draggedBone = DraggedObject as Bone; //make a ref to the dragged object
                    double origX = (_objPreTranslated as Bone).PosX;
                    double origY = (_objPreTranslated as Bone).PosY;
                    double newX = draggedBone.PosX;
                    double newY = draggedBone.PosY;

                    AddUndoAction(new UnRedoPair(() => Move(draggedBone, origX, origY, true), () => Move(draggedBone, newX, newY, false)));
                }
            }
            else if (ScaledTexture != null)
            {
                var scaledTex = ScaledTexture; //make a ref to the dragged object
                double origScaleX = (_objPreTranslated as Texture2D).ScaleX;
                double origScaleY = (_objPreTranslated as Texture2D).ScaleY;
                double newScaleX = scaledTex.ScaleX;
                double newScaleY = scaledTex.ScaleY;

                AddUndoAction(new UnRedoPair(() => Scale(scaledTex, origScaleX, origScaleY, true), () => Scale(scaledTex, newScaleX, newScaleY, false)));
            }
            else if (RotatedObject != null)
            {
                if (_objPreTranslated is Texture2D)
                {
                    var rotatedTex = RotatedObject as Texture2D; //make a ref to the dragged object
                    double origRot = (_objPreTranslated as Texture2D).Rotation;
                    double newRot = rotatedTex.Rotation;

                    AddUndoAction(new UnRedoPair(() => Rotate(rotatedTex, origRot, true), () => Rotate(rotatedTex, newRot, false)));
                }
                else
                {
                    var rotatedBone = RotatedObject as Bone; //make a ref to the dragged object
                    double origRot = (_objPreTranslated as Bone).Rotation;
                    double newRot = rotatedBone.Rotation;

                    AddUndoAction(new UnRedoPair(() => Rotate(rotatedBone, origRot, true), () => Rotate(rotatedBone, newRot, false)));
                }
            }

            _objPreTranslated = null;
            DraggedObject = null;
            ScaledTexture = null;
            RotatedObject = null;
            UpdateSelectedItem();
            Cursor = Cursors.Arrow;
        }

        public void CanvasMouseBtnRDown(MouseEventArgs e)
        {
            if (!IsAnimationMode)
            {
                var itemsControl = e.Source as ItemsControl;
                _mousePosition = new Point(e.GetPosition(itemsControl).X, e.GetPosition(itemsControl).Y); //get mousePos


                //Find all images and bones  that are below the mousepos
                var count = ViewportCollection.Count(item => ((item is Texture2D) && (item as Texture2D).IsMouseOver) 
                    || (item is Bone && (item as Bone).IsMouseOver));

                if (count == 0)
                {
                    foreach (var obj in ViewportCollection)
                    {
                        if (obj is Texture2D) (obj as Texture2D).Selected = false;
                        else if (obj is Bone) (obj as Bone).Selected = false;
                    }
                    UpdateSelectedItem();
                }
                if (_boneDrawn != null)
                {
                    ViewportCollection.Remove(_boneDrawn);
                    _boneDrawn = null;
                }
            }
        }

        public void CanvasMouseMove(MouseEventArgs e)
        {
            if(!IsAnimationMode)
            {
                var itemsControl = e.Source as ItemsControl;
                Point position = new Point(e.GetPosition(itemsControl).X, e.GetPosition(itemsControl).Y);
                if (!IsDrawBonesMode)
                {
                    if (DraggedObject != null)
                    {
                        var offset = position - _mousePosition;
                        _mousePosition = position;
                        if (DraggedObject is Texture2D)
                        {
                            var tex = DraggedObject as Texture2D;
                            tex.PosX = tex.PosX + offset.X;
                            tex.PosY = tex.PosY + offset.Y;
                        }
                        else if (DraggedObject is Bone)
                        {
                            var bone = DraggedObject as Bone;
                            bone.PosX = bone.PosX + offset.X;
                            bone.PosY = bone.PosY + offset.Y;
                        }
                    }
                    else if (ScaledTexture != null)
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            var offset = position - _mousePosition;
                            _mousePosition = position;
                            offset.X /= ScaledTexture.Width;
                            offset.Y /= ScaledTexture.Height;
                            double ratio = offset.X < offset.Y ? offset.X : offset.Y;
                            ScaledTexture.ScaleX += ratio;
                            ScaledTexture.ScaleY += ratio;
                        }
                        else
                        {
                            var offset = position - _mousePosition;
                            _mousePosition = position;
                            offset.X /= ScaledTexture.Width;
                            offset.Y /= ScaledTexture.Height;
                            ScaledTexture.ScaleX += offset.X;
                            ScaledTexture.ScaleY += offset.Y;
                        }
                    }
                    else if (RotatedObject != null)
                    {
                        if (RotatedObject is Texture2D)
                        {
                            Texture2D rotTex = RotatedObject as Texture2D;
                            if (Keyboard.IsKeyDown(Key.LeftShift))
                            {
                                if (rotTex.Rotation%22.5f > 0)
                                    rotTex.Rotation -= (rotTex.Rotation%22.5f);
                                var offset = position - _mousePosition;
                                _mousePosition = position;
                                rotTex.Rotation += 22.5f*Math.Sign(offset.Y);
                            }
                            else
                            {
                                var offset = position - _mousePosition;
                                _mousePosition = position;
                                rotTex.Rotation += offset.Y/2;
                            }
                            if (rotTex.Rotation > 360) rotTex.Rotation %= 360;
                            else if (rotTex.Rotation < 0) rotTex.Rotation = rotTex.Rotation + 360;
                        }
                        else if (RotatedObject is Bone)
                        {
                            Bone rotBone = RotatedObject as Bone;
                            if (Keyboard.IsKeyDown(Key.LeftShift))
                            {
                                if (rotBone.Rotation%22.5f > 0)
                                    rotBone.Rotation -= (rotBone.Rotation%22.5f);
                                var offset = position - _mousePosition;
                                _mousePosition = position;
                                rotBone.Rotation += 22.5f*Math.Sign(offset.Y);
                            }
                            else
                            {
                                var offset = position - _mousePosition;
                                _mousePosition = position;
                                rotBone.Rotation += offset.Y/2;
                            }
                            if (rotBone.Rotation > 360) rotBone.Rotation %= 360;
                            else if (rotBone.Rotation < 0) rotBone.Rotation = rotBone.Rotation + 360;
                        }

                    }
                }
                else
                {
                    _boneDrawn?.UpdateBone(position.X, position.Y);
                }
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data != null && !IsAnimationMode)
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
            var texArr = dropInfo.Data as List<Texture2D>;
            var dataObject = (dropInfo.Data as IDataObject);

            //Undo   
            List<object> undoTexList = new List<object>();

            if (tex != null) //Import one texture from listbox resources
            {
                tex.Zindex = ViewportCollection.Count;
                if (!ViewportCollection.Contains(tex))
                {
                    //Scale Image if larger then canvas
                    var vTex = new Texture2D(tex);
                    double scale = (vTex.Height * vTex.Width) / (WindowHeight * (WindowWidth - 100));
                    if (scale > 1)
                    {
                        vTex.ScaleX /= scale / 2;
                        vTex.ScaleY /= scale / 2;
                    }

                    ViewportCollection.Add(vTex); //add copy of texture2D to viewportcollection
                    undoTexList.Add(vTex);
                }
            }
            else if (texArr != null) //Import multiple texture from listbox resources
            {
                foreach (var texture in texArr)
                {
                    texture.Zindex = ViewportCollection.Count;
                    if (!ViewportCollection.Contains(texture))
                    {
                        //Scale Image if larger then canvas
                        var vTex = new Texture2D(texture);
                        double scale = (vTex.Height * vTex.Width) / (WindowHeight * (WindowWidth - 100));
                        if (scale > 1)
                        {
                            vTex.ScaleX /= scale / 2;
                            vTex.ScaleY /= scale / 2;
                        }
                        ViewportCollection.Add(vTex); //add copy of texture2D to viewportcollection
                        undoTexList.Add(vTex);
                    }
                }
            }
            else if (dataObject != null && dataObject.GetDataPresent(DataFormats.FileDrop, true)) //Import from filexplorer
            {
                string[] filenames = (string[])dataObject.GetData(DataFormats.FileDrop, true);
                foreach (var fName in filenames)
                {
                    var name = Path.GetFileName(fName);
                    if (!string.IsNullOrEmpty(fName))
                    {
                        try
                        {
                            var bitmap = new BitmapImage(new Uri(fName, UriKind.RelativeOrAbsolute));
                            var image = new Image { Source = bitmap };
                            Texture2D texture = new Texture2D
                            {
                                Texture = image,
                                Name = name,
                                Height = (float)image.Source.Height,
                                Width = (float)image.Source.Width,
                                PosX = 0,
                                PosY = 0,
                                ScaleX = 1.0,
                                ScaleY = 1.0,
                                Selected = false
                            };
                            texture.Zindex = ViewportCollection.Count;
                            TextureCollection.Add(texture);

                            //Scale Image if larger then canvas
                            var vTex = new Texture2D(texture);
                            double scale = (vTex.Height * vTex.Width) / (WindowHeight * (WindowWidth - 100));
                            if (scale > 1)
                            {
                                vTex.ScaleX /= scale / 2;
                                vTex.ScaleY /= scale / 2;
                            }

                            ViewportCollection.Add(vTex); 
                            undoTexList.Add(vTex);
                        }
                        catch (Exception)
                        {
                            //await this.ShowMessageAsync("This is the title", "Some message");
                            MessageBox.Show($"There was a problem importing a file!\n" +
                                            $"2D Skeleton Animator does not support files of type {Path.GetExtension(fName)}." +
                                            $"\nOnly files with extension: .jpg, .jpeg, .gif, .bmp, .jpg, .jpeg, .gif, .bmp " +
                                            $"\nare currently supported.", "Error importing file!", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            AddUndoAction(new UnRedoPair(() => UndoDrop(undoTexList), () => { _clearRedoStack = false; RedoDrop(undoTexList); }));
        }

        //----------------
        //UNDO FUNCTIONS--
        //----------------
        public void UndoDrop(List<object> list)
        {
            foreach (var item in list)
            {
                ViewportCollection.Remove(item);
            }
        }

        public void RedoDrop(List<object> list)
        {
            foreach (var item in list)
            {
                ViewportCollection.Add(item);
            }
            AddUndoAction(new UnRedoPair(() => UndoDrop(list), () => { _clearRedoStack = false; RedoDrop(list); }));
        }

        public void Move(object obj, double x, double y, bool undo)
        {
            var tex = obj as Texture2D;
            if (tex != null)
            {
                var newX = tex.PosX;
                var newY = tex.PosY;
                tex.PosX = x;
                tex.PosY = y;
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Move(obj, newX, newY, true), () => Move(obj, x, y, false)));
                }    
            }
            else
            {
                var bone = obj as Bone;
                var newX = bone.PosX;
                var newY = bone.PosY;
                bone.PosX = x;
                bone.PosY = y;
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Move(obj, newX, newY, true), () => Move(obj, x, y, false)));
                }
            }
        }
        public void Scale(object obj, double x, double y, bool undo)
        {
            var tex = obj as Texture2D;
            if (tex != null)
            {
                var newX = tex.ScaleX;
                var newY = tex.ScaleY;
                tex.ScaleX = x;
                tex.ScaleY = y;
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Scale(obj, newX, newY, true), () => Scale(obj, x, y, false)));
                }
            }
            else
            {
                var bone = obj as Bone;
                var newX = bone.ScaleX;
                var newY = bone.ScaleY;
                bone.ScaleX = x;
                bone.ScaleY = y;
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Scale(obj, newX, newY, true), () => Scale(obj, x, y, false)));
                }
            }
        }
        public void Rotate(object obj, double rot, bool undo)
        {
            var tex = obj as Texture2D;
            if (tex != null)
            {
                var newRot = tex.Rotation;
                tex.Rotation = rot;
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Rotate(obj,newRot,true), ()=> Rotate(obj,rot,false)));
                }
            }
            else
            {
                var bone = obj as Bone;
                var newRot = bone.Rotation;
                bone.Rotation = rot;
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Rotate(obj, newRot, true), () => Rotate(obj, rot, false)));
                }
            }
        }

        public void Translate(object obj, double posX, double posY, double scaleX, double scaleY, double rot, bool undo)
        {
            var tex = obj as Texture2D;
            if (tex != null)
            {
                var newPosX = tex.PosX;
                var newPosY = tex.PosY;
                var newScaleX = tex.ScaleX;
                var newScaleY = tex.ScaleY;
                var newRot = tex.Rotation;
                Move(obj, posX, posY,true);
                Scale(obj, scaleX, scaleY, true);
                Rotate(obj, rot, true);
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Translate(obj, newPosX, newPosY,newScaleX,newScaleY,newRot, true), 
                        () => Translate(obj, posX, posY, scaleX, scaleY, rot, false)));
                }
            }
            else
            {
                var bone = obj as Bone;
                var newPosX = bone.PosX;
                var newPosY = bone.PosY;
                var newScaleX = bone.ScaleX;
                var newScaleY = bone.ScaleY;
                var newRot = bone.Rotation;
                Move(obj, posX, posY, true);
                Scale(obj, scaleX, scaleY, true);
                Rotate(obj, rot, true);
                if (!undo)
                {
                    _clearRedoStack = false;
                    AddUndoAction(new UnRedoPair(() => Translate(obj, newPosX, newPosY, newScaleX, newScaleY, newRot, true),
                        () => Translate(obj, posX, posY, scaleX, scaleY, rot, false)));
                }

            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}