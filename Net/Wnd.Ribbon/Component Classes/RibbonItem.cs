// *********************************
// Message from Original Author:
//
// 2008 Jose Menendez Poo
// Please give me credit if you use this code. It's all I ask.
// Contact me for more info: menendezpoo@gmail.com
// *********************************
//
// Original project from http://ribbon.codeplex.com/
// Continue to support and maintain by http://officeribbon.codeplex.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.ComponentModel.Design;

namespace System.Windows.Forms
{
	[DesignTimeVisible(false)]
	public abstract class RibbonItem : Component, IRibbonElement, IRibbonToolTip
	{
		#region Fields
		private string _text;
		private Image _image;
		private bool _checked;
		private bool _selected;
		private Ribbon _owner;
		private Rectangle _bounds;
		private bool _pressed;
		private bool _enabled;
		private object _tag;
		private string _value;
		private string _altKey;
		private RibbonTab _ownerTab;
		private RibbonPanel _ownerPanel;
		private RibbonItem _ownerItem;
		private RibbonElementSizeMode _maxSize;
		private RibbonElementSizeMode _minSize;
		private Size _lastMeasureSize;
		private RibbonElementSizeMode _sizeMode;
		private Control _canvas;
		private bool _visible;
		private RibbonItemTextAlignment _textAlignment;
		private bool _flashEnabled = false;
		private int _flashIntervall = 1000;
		private Image _flashImage;
		private Timer _flashTimer = new Timer();
		protected bool _showFlashImage = false;

		private RibbonToolTip _TT;
		private static RibbonToolTip _lastActiveToolTip;
		private string _tooltip;

		private string _checkedGroup;
		#endregion

		#region enums
		public enum RibbonItemTextAlignment
		{
			Left = StringAlignment.Near,
			Right = StringAlignment.Far,
			Center = StringAlignment.Center
		}
		#endregion

		#region Events
		public virtual event EventHandler DoubleClick;

		public virtual event EventHandler Click;

		public virtual event System.Windows.Forms.MouseEventHandler MouseUp;

		public virtual event System.Windows.Forms.MouseEventHandler MouseMove;

		public virtual event System.Windows.Forms.MouseEventHandler MouseDown;

		public virtual event System.Windows.Forms.MouseEventHandler MouseEnter;

		public virtual event System.Windows.Forms.MouseEventHandler MouseLeave;

		public virtual event EventHandler CanvasChanged;
		public virtual event EventHandler OwnerChanged;

		/// <summary>
		/// Occurs before a ToolTip is initially displayed.
		/// <remarks>Use this event to change the ToolTip or Cancel it at all.</remarks>
		/// </summary>
		public virtual event RibbonElementPopupEventHandler ToolTipPopUp;

		#endregion

		#region Ctor

		public RibbonItem()
		{
			_text = "";
			_enabled = true;
			_visible = true;
			AllowUncheckingAllButtonsInCheckedGroup=true;
			Click += new EventHandler(RibbonItem_Click);
			_flashTimer.Tick += new EventHandler(_flashTimer_Tick);

			//Initialize the ToolTip for this Item
			_TT = new RibbonToolTip(this);
			_TT.InitialDelay = 100;
			_TT.AutomaticDelay = 800;
			_TT.AutoPopDelay = 8000;
			_TT.UseAnimation = true;
			_TT.Active = false;
			_TT.Popup += new PopupEventHandler(_TT_Popup);
		}

      protected override void Dispose(bool disposing)
      {
         if (disposing && RibbonDesigner.Current == null)
         {
            _flashTimer.Enabled = false;

             // ADDED
            _TT.Popup -= _TT_Popup;

            _TT.Dispose();
            if (Image != null)
               Image.Dispose();
         }

         base.Dispose(disposing);
      }

		/// <summary>
		/// Selects the item when in a dropdown, in design mode
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RibbonItem_Click(object sender, EventArgs e)
		{
			RibbonDropDown dd = Canvas as RibbonDropDown;

			if (dd != null && dd.SelectionService != null)
			{
				dd.SelectionService.SetSelectedComponents(
					 new Component[] { this }, System.ComponentModel.Design.SelectionTypes.Primary);

			}
		}

		#endregion

		#region Props

      private string _Name = string.Empty;
      [Browsable(false)]
      [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
      public virtual string Name
      {
         get
         {
            if (base.Site != null)
            {
               _Name = base.Site.Name;
            }
            return _Name;
         }
         set
         {
             if (_Name != value )
                _Name = value;
         }
      }

		/// <summary>
		/// Gets the bounds of the item's content. (It takes the Ribbon.ItemMargin)
		/// </summary>
		/// <remarks>
		/// Although this is the regular item content bounds, it depends on the logic of the item 
		/// and how each item handles its own content.
		/// </remarks>
		[Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual Rectangle ContentBounds
		{
			get
			{
				//Kevin - another point in the designer where an error is thrown when Owner is null
				if (Owner == null) return Rectangle.Empty;

				return Rectangle.FromLTRB(
					 Bounds.Left + Owner.ItemMargin.Left,
					 Bounds.Top + Owner.ItemMargin.Top,
					 Bounds.Right - Owner.ItemMargin.Right,
					 Bounds.Bottom - Owner.ItemMargin.Bottom);
			}
		}

		/// <summary>
		/// Gets the control where the item is currently being dawn
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Control Canvas
		{
			get
			{
				if (_canvas != null && !_canvas.IsDisposed)
					return _canvas;

				return Owner;
			}
		}

      /// <summary>
      /// Gets or sets a value indicating if the Image should be Flashing
      /// </summary>
      [DefaultValue(false)]
      [Category("Flash")]
      public bool FlashEnabled
      {
          get { return _flashEnabled; }
          set 
          {
              if (_flashEnabled != value)
              {
                  _flashEnabled = value;

                  if (_flashEnabled == true)
                  {
                      _showFlashImage = false;
                      _flashTimer.Interval = _flashIntervall;
                      _flashTimer.Enabled = true;
                  }
                  else
                  {
                      _flashTimer.Enabled = false;
                      _showFlashImage = false;
                      NotifyOwnerRegionsChanged();
                  }
              }
          }
      }

      /// <summary>
      /// Gets or sets a value indicating the flashing frequency in Milliseconds
      /// </summary>
      [DefaultValue(1000)]
      [Category("Flash")]
      public int FlashIntervall
      {
          get { return _flashIntervall; }
          set
          {
              if (_flashIntervall != value)
              {
                  _flashIntervall = value;
              }
          }
      }

      [DefaultValue(null)]
      [Category("Flash")]
      public Image FlashImage
      {
          get { return _flashImage; }
          set
          {
              if (_flashImage != value)
              {
                  _flashImage = value;
              }
          }
      }

      [DefaultValue(false)]
      [Browsable(false)]
      public bool ShowFlashImage
      {
          get { return _showFlashImage; }
          set
          {
              if (_showFlashImage != value)
                  _showFlashImage = value;
          }
      }
		/// <summary>
		/// Gets or sets the text that is to be displayed on the item
		/// </summary>
        [DefaultValue(null)]
      [Category("Appearance")]
      [Localizable(true)]
		public virtual string Text
		{
			get
			{
                return _text;
			}
			set
			{
                if (_text != value)
                {
                    _text = value;

                    NotifyOwnerRegionsChanged();
                }
			}
		}

		/// <summary>
		/// Gets or sets the image to be displayed on the item
		/// </summary>
		[DefaultValue(null)]
        [Category("Appearance")]
        public virtual Image Image
		{
			get
			{
				return _image;
			}
			set
			{
				_image = value;

				NotifyOwnerRegionsChanged();
			}
		}

		/// <summary>
		/// Gets or sets the Visibility of this item
		/// </summary>
		[DefaultValue(true)]
        [Category("Behavior")]
        public virtual bool Visible
		{
			get
			{
				if (_visible && Owner != null && !Owner.IsDesignMode())
				{
					if (OwnerItem != null && !OwnerItem.Visible)
						return false;
					if (OwnerPanel != null && !OwnerPanel.Visible)
						return false;
				}
				return _visible;
			}
			set
			{
                if (_visible != value)
                {
                    _visible = value;

                    NotifyOwnerRegionsChanged();
                }
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if the item is currently checked
		/// </summary>
		[DefaultValue(false)]
		[Category("Appearance")]
		[Description("Indicates whether the component is in the checked state.")]
		public virtual bool Checked
		{
			get
			{
				return _checked;
			}
			set
			{
				if(_checked!=value)
				{
					if(_checkedGroup!=null)
					{
						// Kevin Carbis - implementing the CheckGroup property logic.  This will uncheck all the other buttons in this group
						// Shintadono - added RibbonItemGroup implementation
						if(value)
						{
							_checked=true;
							if(Canvas is RibbonDropDown)
							{
								foreach(RibbonItem itm in ((RibbonDropDown)Canvas).Items)
								{
									if(itm.CheckedGroup==_checkedGroup&&itm.Checked==true&&itm!=this)
									{
										itm._checked=false;
										itm.RedrawItem();
									}
								}
							}
							else if((_ownerItem!=null)&&(_ownerItem is RibbonItemGroup))
							{
								RibbonItemGroup group=_ownerItem as RibbonItemGroup;
								foreach(RibbonItem itm in group.Items)
								{
									if(itm.CheckedGroup==_checkedGroup&&itm.Checked==true&&itm!=this)
									{
										itm._checked=false;
										itm.RedrawItem();
									}
								}
							}
							else if(_ownerPanel!=null)
							{
								foreach(RibbonItem itm in _ownerPanel.Items)
								{
									if(itm.CheckedGroup==_checkedGroup&&itm.Checked==true&&itm!=this)
									{
										itm._checked=false;
										itm.RedrawItem();
									}
								}
							}
						}
						else
						{
							// Shintadono - extended Kevins implementation to not allow unchecking all buttons of a checked group.
							// (IMPORTANT: If one button of a checked group doesn't allow unchecking all buttons, the whole group doesn't allows it.)
							bool isNotAllowed=false;

							if(Canvas is RibbonDropDown)
							{
								foreach(RibbonItem itm in ((RibbonDropDown)Canvas).Items)
								{
									if(itm.CheckedGroup==_checkedGroup&&!itm.AllowUncheckingAllButtonsInCheckedGroup)
									{
										isNotAllowed=true; break;
									}
								}
							}
							else if((_ownerItem!=null)&&(_checkedGroup!=null)&&(_ownerItem is RibbonItemGroup))
							{
								RibbonItemGroup group=_ownerItem as RibbonItemGroup;
								foreach(RibbonItem itm in group.Items)
								{
									if(itm.CheckedGroup==_checkedGroup&&!itm.AllowUncheckingAllButtonsInCheckedGroup)
									{
										isNotAllowed=true; break;
									}
								}
							}
							else if((_ownerPanel!=null)&&(_checkedGroup!=null))
							{
								foreach(RibbonItem itm in _ownerPanel.Items)
								{
									if(itm.CheckedGroup==_checkedGroup&&!itm.AllowUncheckingAllButtonsInCheckedGroup)
									{
										isNotAllowed=true; break;
									}
								}
							}

							_checked=isNotAllowed;
						}
					}
					else _checked=value;

					NotifyOwnerRegionsChanged();
				}
			}
		}

		/// <summary>
		/// Determines the other Ribbon Items that belong to this checked group.  When one button is checked the other items in this group will be unchecked automatically.  This only applies to Items that are within the same Ribbon Panel or Dropdown Window.
		/// </summary>
		/// <remarks></remarks>
		[DefaultValue(null)]
        [Category("Behavior")]
		[Description("Determines the other Ribbon Items that belong to this checked group.  When one button is checked the other items in this group will be unchecked automatically.  This only applies to Items that are within the same Parent")]
		public virtual string CheckedGroup
		{
			get
			{
				return _checkedGroup;
			}
			set
			{
                if ( _checkedGroup != value )
    				_checkedGroup = value;
			}
		}

		[DefaultValue(true)]
		[Description("Do not allow unchecking all buttons of a checked group. If one button of a checked group doesn't allow unchecking all buttons, the whole group doesn't allows it.")]
		public virtual bool AllowUncheckingAllButtonsInCheckedGroup { get; set; }

		/// <summary>
		/// Gets the item's current SizeMode
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RibbonElementSizeMode SizeMode
		{
			get { return _sizeMode; }
		}

		/// <summary>
		/// Gets a value indicating whether the item is selected (moused over)
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual bool Selected
		{
			get
			{
				return _selected;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the state of the item is pressed
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual bool Pressed
		{
			get
			{
				return _pressed;
			}
		}

		/// <summary>
		/// Gets the Ribbon owner of this item
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Ribbon Owner
		{
			get
			{
				return _owner;
			}
		}

		/// <summary>
		/// Gets the bounds of the element relative to the Ribbon control
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Rectangle Bounds
		{
			get
			{
				return _bounds;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if the item is currently enabled
		/// </summary>
		[DefaultValue(true)]
        [Category("Behavior")]
        public virtual bool Enabled
		{
			get
			{
				if (Owner != null)
				{
					return _enabled && Owner.Enabled;
				}
				else
				{
					return _enabled;
				}
			}
			set
			{
                if (_enabled != value)
                {
                    _enabled = value;

                    IContainsSelectableRibbonItems container = this as IContainsSelectableRibbonItems;

                    if (container != null)
                    {
                        foreach (RibbonItem item in container.GetItems())
                        {
                            item.Enabled = value;
                        }
                    }

                    NotifyOwnerRegionsChanged();
                }
			}
		}

		/// <summary>
		/// Gets or sets the tool tip title
		/// </summary>
		[DefaultValue("")]
		public string ToolTipTitle
		{
			get
			{
				return _TT.ToolTipTitle;
			}
			set
			{
                if ( _TT.ToolTipTitle != value )
    				_TT.ToolTipTitle = value;
			}
		}

		/// <summary>
		/// Gets or sets the image of the tool tip
		/// </summary>
		[DefaultValue(ToolTipIcon.None)]
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ToolTipIcon ToolTipIcon
		{
			get
			{
				return _TT.ToolTipIcon;
			}
			set
			{
                if ( _TT.ToolTipIcon != value )
	    			_TT.ToolTipIcon = value;
   			}
		}

		/// <summary>
		/// Gets or sets the tool tip text
		/// </summary>
		[DefaultValue(null)]
		[Localizable(true)]
		public string ToolTip
		{
			get
			{
                return _tooltip;
			}
			set
			{
                if ( _tooltip != value )
				_tooltip = value;
			}
		}

		/// <summary>
		/// Gets or sets the tool tip image
		/// </summary>
		[DefaultValue(null)]
		[Localizable(true)]
		public Image ToolTipImage
		{
			get
			{
				return _TT.ToolTipImage;
			}
			set
			{
                if ( _TT.ToolTipImage != value )
    				_TT.ToolTipImage = value;
			}
		}

		/// <summary>
		/// Gets or sets the custom object data associated with this control
		/// </summary>
		[DescriptionAttribute("An Object field for associating custom data for this control")]
		[DefaultValue(null)]
        [Category("Data")]
        [TypeConverter(typeof(StringConverter))]
      public object Tag
		{
			get
			{
				return _tag;
			}
			set
			{
                if ( _tag != value )
    				_tag = value;
			}
		}

		/// <summary>
		/// Gets or sets the custom string data associated with this control
		/// </summary>
		[DefaultValue(null)]
        [Category("Data")]
        [DescriptionAttribute("A string field for associating custom data for this control")]
		public string Value
		{
			get
			{
				return _value;
			}
			set
			{
                if ( _value != value )
    				_value = value;
			}
		}

		/// <summary>
		/// Gets or sets the key combination that activates this element when the Alt key was pressed
		/// </summary>
        [DefaultValue(null)]
        [Category("Behavior")]
        public string AltKey
		{
			get
			{
				return _altKey;
			}
			set
			{
                if ( _altKey != value )
    				_altKey = value;
			}
		}

		/// <summary>
		/// Gets the RibbonTab that contains this item
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RibbonTab OwnerTab
		{
			get
			{
				return _ownerTab;
			}
		}

		/// <summary>
		/// Gets the RibbonPanel where this item is located
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RibbonPanel OwnerPanel
		{
			get
			{
				return _ownerPanel;
			}
		}

        /// <summary>
        /// Gets the RibbonItem that owns the item (If any)
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RibbonItem OwnerItem
        {
            get { return _ownerItem; }
        }

		/// <summary>
		/// Gets or sets the maximum size mode of the element
		/// </summary>
		[DefaultValue(RibbonElementSizeMode.None)]
        [Category("Appearance")]
        [Description("Sets the maximum size mode of the element.")]
        public RibbonElementSizeMode MaxSizeMode
		{
			get
			{
				return _maxSize;
			}
			set
			{
                if (_maxSize != value)
                {
                    _maxSize = value;

                    NotifyOwnerRegionsChanged();
                }
			}
		}

		/// <summary>
		/// Gets or sets the minimum size mode of the element
		/// </summary>
		[DefaultValue(RibbonElementSizeMode.None)]
        [Category("Appearance")]
        [Description("Sets the minimum size mode of the element.")]
        public RibbonElementSizeMode MinSizeMode
		{
			get
			{
				return _minSize;
			}
			set
			{
                if (_minSize != value)
                {
                    _minSize = value;

                    NotifyOwnerRegionsChanged();
                }
			}
		}

		/// <summary>
		/// Gets the last result of  MeasureSize
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Size LastMeasuredSize
		{
			get
			{
				return _lastMeasureSize;
			}
		}
		/// <summary>
		/// Sets the alignment of the label text if it exists
		/// </summary>
		[DefaultValue(RibbonItemTextAlignment.Left)]
        [Category("Appearance")]
        public RibbonItemTextAlignment TextAlignment
		{
			get { return _textAlignment; }
			set 
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    NotifyOwnerRegionsChanged();
                }
            }
		}

        #endregion

        #region Flashtimer

        void _flashTimer_Tick(object sender, EventArgs e)
        {
            _showFlashImage = !_showFlashImage;
            NotifyOwnerRegionsChanged();
        }

        #endregion

		#region Methods

		/// <summary>
		/// Gets if owner dropdown must be closed when the item is clicked on the specified point
		/// </summary>
		/// <param name="p">Point to test.</param>
		/// <returns></returns>
		protected virtual bool ClosesDropDownAt(Point p)
		{
			return true;
		}

		/// <summary>
		/// Forces the owner Ribbon to update its regions
		/// </summary>
		protected void NotifyOwnerRegionsChanged()
		{
			if (Owner != null)
			{
				if (Owner == Canvas)
				{
					Owner.OnRegionsChanged();
				}
				else if (Canvas != null)
				{
					if (Canvas is RibbonOrbDropDown)
					{
						(Canvas as RibbonOrbDropDown).OnRegionsChanged();
					}
					else
					{
						Canvas.Invalidate(Bounds);
					}
				}
			}
		}

        /// <summary>
        /// Sets the value of the OwnerItem property
        /// </summary>
        /// <param name="item">RibbonItem where this item is located</param>
        internal virtual void SetOwnerItem(RibbonItem item)
        {
            _ownerItem = item;
        }

		/// <summary>
		/// Sets the Ribbon that owns this item
		/// </summary>
		/// <param name="owner">Ribbon that owns this item</param>
		internal virtual void SetOwner(Ribbon owner)
		{
			_owner = owner;
			OnOwnerChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Sets the value of the OwnerPanel property
		/// </summary>
		/// <param name="ownerPanel">RibbonPanel where this item is located</param>
		internal virtual void SetOwnerPanel(RibbonPanel ownerPanel)
		{
			_ownerPanel = ownerPanel;
		}

		/// <summary>
		/// Sets the value of the Selected property
		/// </summary>
		/// <param name="selected">Value that indicates if the element is selected</param>
		internal virtual void SetSelected(bool selected)
		{
			if (!Enabled) return;

			_selected = selected;
		}

		/// <summary>
		/// Sets the value of the Pressed property
		/// </summary>
		/// <param name="pressed">Value that indicates if the element is pressed</param>
		internal virtual void SetPressed(bool pressed)
		{
			_pressed = pressed;
		}

		/// <summary>
		/// Sets the value of the OwnerTab property
		/// </summary>
		/// <param name="ownerTab">RibbonTab where this item is located</param>
		internal virtual void SetOwnerTab(RibbonTab ownerTab)
		{
			_ownerTab = ownerTab;
		}

		/// <summary>
		/// When an item is removed from the RibbonItemCollection remove all its references.
		/// </summary>
		internal virtual void ClearOwner()
		{
			_ownerItem = null;
			_ownerPanel = null;
			_ownerTab = null;
			_owner = null;
			OnOwnerChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Gets the size applying the rules of MaxSizeMode and MinSizeMode properties
		/// </summary>
		/// <param name="sizeMode">Suggested sizeMode</param>
		/// <returns>The nearest size to the specified one</returns>
		protected RibbonElementSizeMode GetNearestSize(RibbonElementSizeMode sizeMode)
		{
			int size = (int)sizeMode;
			int max = (int)MaxSizeMode;
			int min = (int)MinSizeMode;
			int result = (int)sizeMode;

			if (max > 0 && size > max) //Max is specified and value exceeds max
			{
				result = max;
			}

			if (min > 0 && size < min) //Min is specified and value exceeds min
			{
				result = min;
			}

			return (RibbonElementSizeMode)result;
		}

		/// <summary>
		/// Sets the value of the LastMeasuredSize property
		/// </summary>
		/// <param name="size">Size to set to the property</param>
		protected void SetLastMeasuredSize(Size size)
		{
			_lastMeasureSize = size;
		}

		/// <summary>
		/// Sets the value of the SizeMode property
		/// </summary>
		/// <param name="sizeMode"></param>
		internal virtual void SetSizeMode(RibbonElementSizeMode sizeMode)
		{
			_sizeMode = GetNearestSize(sizeMode);
		}

		/// <summary>
		/// Raises the <see cref="CanvasChanged"/> event
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnCanvasChanged(EventArgs e)
		{
			if (CanvasChanged != null)
			{
				CanvasChanged(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="OwnerChanged"/> event
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnOwnerChanged(EventArgs e)
		{
			if (OwnerChanged != null)
			{
				OwnerChanged(this, e);
			}
		}

		/// <summary>
		/// Raises the MouseEnter event
		/// </summary>
		/// <param name="e">Event data</param>
		public virtual void OnMouseEnter(MouseEventArgs e)
		{
			if (!Enabled) return;

			if (MouseEnter != null)
			{
				MouseEnter(this, e);
			}
		}

		/// <summary>
		/// Raises the MouseDown event
		/// </summary>
		/// <param name="e">Event data</param>
		public virtual void OnMouseDown(MouseEventArgs e)
		{
			if (!Enabled) return;

			if (MouseDown != null)
			{
				MouseDown(this, e);
			}

			//RibbonPopup pop = Canvas as RibbonPopup;

			//if (pop != null)
			//{
			//   if (ClosesDropDownAt(e.Location))
			//   {
			//      RibbonPopupManager.Dismiss(RibbonPopupManager.DismissReason.ItemClicked);
			//   }
			//OnClick(EventArgs.Empty);
			//}

			SetPressed(true);
		}

		/// <summary>
		/// Raises the MouseLeave event
		/// </summary>
		/// <param name="e">Event data</param>
		public virtual void OnMouseLeave(MouseEventArgs e)
		{
			if (!Enabled) return;

			DeactivateToolTip(_TT);

			if (MouseLeave != null)
			{
				MouseLeave(this, e);
			}
		}

		/// <summary>
		/// Raises the MouseUp event
		/// </summary>
		/// <param name="e">Event data</param>
		public virtual void OnMouseUp(MouseEventArgs e)
		{
			if (!Enabled) return;

			if (MouseUp != null)
			{
				MouseUp(this, e);
			}

			if (Pressed)
			{
				SetPressed(false);
				RedrawItem();
			}
		}

		/// <summary>
		/// Raises the MouseMove event
		/// </summary>
		/// <param name="e">Event data</param>
      public virtual void OnMouseMove(MouseEventArgs e)
      {
         if (!Enabled) return;

         if (MouseMove != null)
         {
            MouseMove(this, e);
         }

         //Kevin - found cases where mousing into buttons doesn't set the selection. This arose with the office 2010 style
         if (!Selected)
         { SetSelected(true); Owner.Invalidate(this.Bounds); }

         if (!_TT.Active && !string.IsNullOrEmpty(this.ToolTip))  // ToolTip should be working without title as well - to get Office 2007 Look & Feel
         {
            DeactivateToolTip(_lastActiveToolTip);
            if (this.ToolTip != _TT.GetToolTip(this.Canvas))
            {
               _TT.SetToolTip(this.Canvas, this.ToolTip);
            }
            _TT.Active = true;
            _lastActiveToolTip = null;
            _lastActiveToolTip = _TT;
         }
      }

		/// <summary>
		/// Raises the Click event
		/// </summary>
		/// <param name="e">Event data</param>
		public virtual void OnClick(EventArgs e)
		{
			if (!Enabled) return;

			if (ClosesDropDownAt(Canvas.PointToClient(Cursor.Position)))
			{
				DeactivateToolTip(_TT);
				RibbonPopupManager.Dismiss(RibbonPopupManager.DismissReason.ItemClicked);
			}

         SetSelected(false);

         if (Click != null)
			{
				Click(this, e);
			}
      }

		/// <summary>
		/// Raises the DoubleClick event
		/// </summary>
		/// <param name="e">Event data</param>
		public virtual void OnDoubleClick(EventArgs e)
		{
			if (!Enabled) return;

			if (DoubleClick != null)
			{
				DoubleClick(this, e);
			}
      }

		/// <summary>
		/// Redraws the item area on the Onwer Ribbon
		/// </summary>
		public virtual void RedrawItem()
		{
			if (Canvas != null)
			{
				Canvas.Invalidate(Rectangle.Inflate(Bounds, 1, 1));
			}
		}

		/// <summary>
		/// Sets the canvas of the item
		/// </summary>
		/// <param name="canvas"></param>
		internal void SetCanvas(Control canvas)
		{
			_canvas = canvas;

			SetCanvas(this as IContainsSelectableRibbonItems, canvas);

			OnCanvasChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Recurse on setting the canvas
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="canvas"></param>
		private void SetCanvas(IContainsSelectableRibbonItems parent, Control canvas)
		{
			if (parent == null) return;

			foreach (RibbonItem item in parent.GetItems())
			{
				item.SetCanvas(canvas);
			}
		}

      private void _TT_Popup(object sender, PopupEventArgs e)
      {
         if (ToolTipPopUp != null)
         {
            ToolTipPopUp(sender, new RibbonElementPopupEventArgs(this, e));
            if (this.ToolTip != _TT.GetToolTip(this.Canvas))
               _TT.SetToolTip(this.Canvas, this.ToolTip);
         }
      }

		private void DeactivateToolTip(RibbonToolTip toolTip)
		{
			if (toolTip == null)
				return;

			toolTip.Active = false;
			toolTip.RemoveAll();  // this is needed otherwise a tooltip within a dropdown is not shown again if the item is clicked
		}
		#endregion

		#region IRibbonElement Members

		public abstract void OnPaint(object sender, RibbonElementPaintEventArgs e);

		public virtual void SetBounds(Rectangle bounds)
		{
			_bounds = bounds;
		}

		public abstract Size MeasureSize(object sender, RibbonElementMeasureSizeEventArgs e);

		#endregion

	}
}
