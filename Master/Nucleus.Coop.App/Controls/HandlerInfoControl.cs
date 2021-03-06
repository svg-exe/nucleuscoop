﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nucleus.Gaming.Coop.Api;
using Nucleus.Gaming;
using Nucleus.Gaming.Platform.Windows.Controls;
using Nucleus.Gaming.Package;

namespace Nucleus.Coop.App.Controls {
    public partial class HandlerInfoControl : UserControl, IDynamicSized, IRadioControl, IMouseHoverControl {
        public Color SelectedColor { get; set; } = Color.FromArgb(80, 80, 80);
        public Color NotSelectedColor { get; set; } = Color.FromArgb(30, 30, 30);
        public Color HoverColor { get; set; } = Color.FromArgb(60, 60, 60);

        public event Action<HandlerInfoControl> OnSelected;

        public IgdbGame IgdbGame { get; private set; }
        public Game Game { get; private set; }
        public GameHandler Handler { get; private set; }
        public GameHandlerBaseMetadata Metadata { get; private set; }
        public bool EnableClicking { get; set; } = true;

        private TransparentControl mouseControl;
        public TransparentControl Mouse { get { return mouseControl; } }

        public HandlerInfoControl() {
            InitializeComponent();

            mouseControl = new TransparentControl();
            mouseControl.Size = this.Size;

            this.Controls.Add(mouseControl);

            this.BackColor = NotSelectedColor;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);

            if (mouseControl != null) {
                mouseControl.Size = this.Size;
                mouseControl.BringToFront();
            }
        }

        protected override void OnControlAdded(ControlEventArgs e) {
            base.OnControlAdded(e);

            if (mouseControl != null
                && e.Control != mouseControl) {
                mouseControl.BringToFront();
            }
        }

        public void SetEmptyHandler() {
            lbl_handlerName.Text = "No available handlers";
        }

        public void SetHandler(GameHandlerBaseMetadata metadata) {
            this.Metadata = metadata;

            lbl_handlerName.Text = metadata.Title;
            lbl_version.Text = metadata.V.ToString();
            lbl_description.Text = metadata.LastUpdate.ToString();
            lbl_devName.Text = metadata.Dev;
        }

        public void SetHandler(IgdbGame game) {
            this.IgdbGame = game;

            lbl_handlerName.Text = game.name;
            lbl_version.Text = game.release_dates?.First().date;
            lbl_description.Text = game.summary;
            lbl_devName.Text = "";
        }

        public void SetHandler(Game game) {
            this.Game = game;

            lbl_handlerName.Text = game.name;
            lbl_version.Text = game.updatedAt;
            lbl_description.Text = game.summary;
            lbl_devName.Text = "";
        }

        public void SetHandler(GameHandler handler) {
            this.Handler = handler;

            lbl_handlerName.Text = handler.name;
            lbl_version.Text = handler.currentVersion.ToString();
            lbl_description.Text = handler.details;
            lbl_devName.Text = handler.owner.ToString();
        }

        private bool isSelected;
        public void RadioSelected() {
            if (!isSelected &&
                OnSelected != null) {
                isSelected = true;
                OnSelected(this);
            }

            isSelected = true;
            BackColor = SelectedColor;
        }

        public void RadioUnselected() {
            BackColor = NotSelectedColor;
            isSelected = false;
        }

        public void UpdateSize(float scale) {
        }

        public void UserLeave() {
            BackColor = isSelected ? SelectedColor : NotSelectedColor;
        }

        public void UserOver() {
            BackColor = HoverColor;
        }
    }
}
