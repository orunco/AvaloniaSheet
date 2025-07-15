using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.VisualTree;

// 20240917 copy from avalonia 11.1.3
// @formatter:off
namespace AvaloniaSheetControl.Porting
{
    /// <summary>
    /// Base class for controls that can contain multiple children.
    /// </summary>
    /// <remarks>
    /// Controls can be added to a <see cref="PanelX"/> by adding them to its <see cref="Children"/>
    /// collection. All children are layed out to fill the PanelX.
    /// </remarks>
    public class PanelX : Control, IChildIndexProvider
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<PanelX>();

        /// <summary>
        /// Initializes static members of the <see cref="PanelX"/> class.
        /// </summary>
        static PanelX()
        {
            AffectsRender<PanelX>(BackgroundProperty);
        }

        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="PanelX"/> class.
        /// </summary>
        public PanelX()
        {
            Children.CollectionChanged += ChildrenChanged;
        }

        /// <summary>
        /// Gets the children of the <see cref="PanelX"/>.
        /// </summary>
        [Content]
        public Controls Children { get; } = new Controls();

        /// <summary>
        /// Gets or Sets PanelX background brush.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets whether the <see cref="PanelX"/> hosts the items created by an <see cref="ItemsPresenter"/>.
        /// </summary>
        public bool IsItemsHost { get; internal set; }

        event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
        {
            add
            {
                if (_childIndexChanged is null)
                    Children.PropertyChanged += ChildrenPropertyChanged;
                _childIndexChanged += value;
            }

            remove
            {
                _childIndexChanged -= value;
                if (_childIndexChanged is null)
                    Children.PropertyChanged -= ChildrenPropertyChanged;
            }
        }

        /// <summary>
        /// Renders the visual to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// 必须去掉 sealed
        public override void Render(DrawingContext context)
        {
            var background = Background;
            if (background != null)
            {
                var renderSize = Bounds.Size;
                context.FillRectangle(background, new Rect(renderSize));
            }

            base.Render(context);
        }

        /// <summary>
        /// Marks a property on a child as affecting the parent PanelX's arrangement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        protected static void AffectsParentArrange<TPanelX>(params AvaloniaProperty[] properties)
            where TPanelX : PanelX
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => AffectsParentArrangeInvalidate<TPanelX>(e));
            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Marks a property on a child as affecting the parent PanelX's measurement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        protected static void AffectsParentMeasure<TPanelX>(params AvaloniaProperty[] properties)
            where TPanelX : PanelX
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => AffectsParentMeasureInvalidate<TPanelX>(e));
            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Called when the <see cref="Children"/> collection changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (!IsItemsHost)
                    {
                        LogicalChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Control>().ToList());
                    }
                    VisualChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (!IsItemsHost)
                    {
                        LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    }
                    VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (!IsItemsHost)
                    {
                        LogicalChildren.RemoveAll(e.OldItems!.OfType<Control>().ToList());
                    }
                    VisualChildren.RemoveAll(e.OldItems!.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (var i = 0; i < e.OldItems!.Count; ++i)
                    {
                        var index = i + e.OldStartingIndex;
                        var child = (Control)e.NewItems![i]!;
                        if (!IsItemsHost)
                        {
                            LogicalChildren[index] = child;
                        }
                        VisualChildren[index] = child;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }

            _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
            InvalidateMeasureOnChildrenChanged();
        }

        private protected virtual void InvalidateMeasureOnChildrenChanged()
        {
            InvalidateMeasure();
        }

        private void ChildrenPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Children.Count) || e.PropertyName is null)
                _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.TotalCountChanged);
        }

        private static void AffectsParentArrangeInvalidate<TPanelX>(AvaloniaPropertyChangedEventArgs e)
            where TPanelX : PanelX
        {
            var control = e.Sender as Control;
            var panelX = control?.GetVisualParent() as TPanelX;
            panelX?.InvalidateArrange();
        }

        private static void AffectsParentMeasureInvalidate<TPanelX>(AvaloniaPropertyChangedEventArgs e)
            where TPanelX : PanelX
        {
            var control = e.Sender as Control;
            var panelX = control?.GetVisualParent() as TPanelX;
            panelX?.InvalidateMeasure();
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return child is Control control ? Children.IndexOf(control) : -1;
        }

        /// <inheritdoc />
        bool IChildIndexProvider.TryGetTotalCount(out int count)
        {
            count = Children.Count;
            return true;
        }
    }
}
