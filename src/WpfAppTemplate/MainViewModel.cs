using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Animations;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Microsoft.Win32;
using SharpDX;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Point3D = System.Windows.Media.Media3D.Point3D;

namespace WpfAppTemplate;

public class MainViewModel : BaseViewModel
{
    private bool showWireframe = false;

    public bool ShowWireframe
    {
        set
        {
            if (SetValue(ref showWireframe, value))
            {
                ShowWireframeFunct(value);
            }
        }
        get
        {
            return showWireframe;
        }
    }

    private bool renderFlat = false;

    public bool RenderFlat
    {
        set
        {
            if (SetValue(ref renderFlat, value))
            {
                RenderFlatFunct(value);
            }
        }
        get
        {
            return renderFlat;
        }
    }

    private bool renderEnvironmentMap = true;

    public bool RenderEnvironmentMap
    {
        set
        {
            if (SetValue(ref renderEnvironmentMap, value) && scene != null && scene.Root != null)
            {
                foreach (var node in scene.Root.Traverse())
                {
                    if (node is MaterialGeometryNode m && m.Material is PBRMaterialCore material)
                    {
                        material.RenderEnvironmentMap = value;
                    }
                }
            }
        }
        get => renderEnvironmentMap;
    }

    public ICommand ResetCameraCommand
    {
        set; get;
    }

    public ICommand ExportCommand { private set; get; }

    public ICommand CopyAsBitmapCommand { private set; get; }

    public ICommand CopyAsHiresBitmapCommand { private set; get; }

    private bool isLoading = false;

    public bool IsLoading
    {
        private set => SetValue(ref isLoading, value);
        get => isLoading;
    }

    private bool enableAnimation = false;

    public bool EnableAnimation
    {
        set
        {
            if (SetValue(ref enableAnimation, value))
            {
                if (value)
                {
                    StartAnimation();
                }
                else
                {
                    StopAnimation();
                }
            }
        }
        get { return enableAnimation; }
    }

    public ObservableCollection<IAnimationUpdater> Animations { get; } = new ObservableCollection<IAnimationUpdater>();

    public SceneNodeGroupModel3D GroupModel { get; } = new SceneNodeGroupModel3D();

    private IAnimationUpdater selectedAnimation = null;

    public IAnimationUpdater SelectedAnimation
    {
        set
        {
            if (SetValue(ref selectedAnimation, value))
            {
                StopAnimation();
                if (value != null)
                {
                    animationUpdater = value;
                    animationUpdater.Reset();
                    animationUpdater.RepeatMode = AnimationRepeatMode.Loop;
                    animationUpdater.Speed = Speed;
                }
                else
                {
                    animationUpdater = null;
                }
                if (enableAnimation)
                {
                    StartAnimation();
                }
            }
        }
        get
        {
            return selectedAnimation;
        }
    }

    private float speed = 1.0f;

    public float Speed
    {
        set
        {
            if (SetValue(ref speed, value))
            {
                if (animationUpdater != null)
                    animationUpdater.Speed = value;
            }
        }
        get => speed;
    }

    private Point3D modelCentroid = default;

    public Point3D ModelCentroid
    {
        private set => SetValue(ref modelCentroid, value);
        get => modelCentroid;
    }

    private BoundingBox modelBound = new BoundingBox();

    public BoundingBox ModelBound
    {
        private set => SetValue(ref modelBound, value);
        get => modelBound;
    }

    public TextureModel EnvironmentMap { get; }

    private HelixToolkitScene scene;
    private IAnimationUpdater animationUpdater;
    private CompositionTargetEx compositeHelper = new CompositionTargetEx();

    public MainViewModel()
    {
        EffectsManager = new DefaultEffectsManager();
        Camera = new OrthographicCamera()
        {
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, -10),
            Position = new System.Windows.Media.Media3D.Point3D(0, 10, 10),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            FarPlaneDistance = 5000,
            NearPlaneDistance = 0.1f
        };
        ResetCameraCommand = new DelegateCommand(() =>
        {
            (Camera as OrthographicCamera).Reset();
            (Camera as OrthographicCamera).FarPlaneDistance = 5000;
            (Camera as OrthographicCamera).NearPlaneDistance = 0.1f;
        });

        EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");
    }

    public void LoadFile(string path)
    {
        StopAnimation();
        var syncContext = SynchronizationContext.Current;
        IsLoading = true;
        Task.Run(() =>
        {
            var loader = new Importer();
            var scene = loader.Load(path);
            scene.Root.Attach(EffectsManager); // Pre attach scene graph
            scene.Root.UpdateAllTransformMatrix();
            if (scene.Root.TryGetBound(out var bound))
            {
                /// Must use UI thread to set value back.
                syncContext.Post((o) => { ModelBound = bound; }, null);
            }
            if (scene.Root.TryGetCentroid(out var centroid))
            {
                /// Must use UI thread to set value back.
                syncContext.Post((o) => { ModelCentroid = centroid.ToPoint3D(); }, null);
            }
            return scene;
        }).ContinueWith((result) =>
        {
            IsLoading = false;
            if (result.IsCompleted)
            {
                scene = result.Result;
                Animations.Clear();
                var oldNode = GroupModel.SceneNode.Items.ToArray();
                GroupModel.Clear(false);
                Task.Run(() =>
                {
                    foreach (var node in oldNode)
                    { node.Dispose(); }
                });
                if (scene != null)
                {
                    if (scene.Root != null)
                    {
                        foreach (var node in scene.Root.Traverse())
                        {
                            if (node is MaterialGeometryNode m)
                            {
                                //m.Geometry.SetAsTransient();
                                if (m.Material is PBRMaterialCore pbr)
                                {
                                    pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                }
                                else if (m.Material is PhongMaterialCore phong)
                                {
                                    phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                }
                            }
                        }
                    }
                    GroupModel.AddNode(scene.Root);
                    if (scene.HasAnimation)
                    {
                        var dict = scene.Animations.CreateAnimationUpdaters();
                        foreach (var ani in dict.Values)
                        {
                            Animations.Add(ani);
                        }
                    }
                    foreach (var n in scene.Root.Traverse())
                    {
                        n.Tag = new AttachedNodeViewModel(n);
                    }
                    FocusCameraToScene();
                }
            }
            else if (result.IsFaulted && result.Exception != null)
            {
                MessageBox.Show(result.Exception.Message);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void StartAnimation()
    {
        compositeHelper.Rendering += CompositeHelper_Rendering;
    }

    public void StopAnimation()
    {
        compositeHelper.Rendering -= CompositeHelper_Rendering;
    }

    private void CompositeHelper_Rendering(object sender, System.Windows.Media.RenderingEventArgs e)
    {
        if (animationUpdater != null)
        {
            animationUpdater.Update(Stopwatch.GetTimestamp(), Stopwatch.Frequency);
        }
    }

    private void FocusCameraToScene()
    {
        var maxWidth = Math.Max(Math.Max(modelBound.Width, modelBound.Height), modelBound.Depth);
        var pos = modelBound.Center + new Vector3(0, 0, maxWidth);
        Camera.Position = pos.ToPoint3D();
        Camera.LookDirection = (modelBound.Center - pos).ToVector3D();
        Camera.UpDirection = Vector3.UnitY.ToVector3D();
        if (Camera is OrthographicCamera orthCam)
        {
            orthCam.Width = maxWidth;
        }
    }

    private string OpenFileDialog(string filter)
    {
        var d = new OpenFileDialog();
        d.CustomPlaces.Clear();

        d.Filter = filter;

        if (!d.ShowDialog().Value)
        {
            return null;
        }

        return d.FileName;
    }

    private void ShowWireframeFunct(bool show)
    {
        foreach (var node in GroupModel.GroupNode.Items.PreorderDFT((node) =>
        {
            return node.IsRenderable;
        }))
        {
            if (node is MeshNode m)
            {
                m.RenderWireframe = show;
            }
        }
    }

    private void RenderFlatFunct(bool show)
    {
        foreach (var node in GroupModel.GroupNode.Items.PreorderDFT((node) =>
        {
            return node.IsRenderable;
        }))
        {
            if (node is MeshNode m)
            {
                if (m.Material is PhongMaterialCore phong)
                {
                    phong.EnableFlatShading = show;
                }
                else if (m.Material is PBRMaterialCore pbr)
                {
                    pbr.EnableFlatShading = show;
                }
            }
        }
    }
}