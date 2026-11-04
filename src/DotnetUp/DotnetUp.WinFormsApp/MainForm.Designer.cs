using System.Diagnostics.CodeAnalysis;

namespace DotnetUp.WinFormsApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        object IServiceProvider.GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            return _serviceProvider.GetService(serviceType)
                ?? throw new InvalidOperationException(
                    $"Service of type '{serviceType.Name}' is not registered.");
        }

#pragma warning disable WFO3001
        /// <summary>
        ///  Defers service resolution until a Designer-created component requests it.
        /// </summary>
        private sealed class DeferredServiceProvider : IServiceProvider
        {
            private readonly Func<IServiceProvider> _serviceProviderFactory;

            public DeferredServiceProvider(IServiceProvider serviceProvider)
            {
                _serviceProviderFactory = () => serviceProvider;
            }

            public object GetService(Type serviceType)
            {
                return _serviceProviderFactory().GetService(serviceType)
                    ?? throw new InvalidOperationException(
                        $"Service of type '{serviceType.Name}' is not registered.");
            }
        }
        
#pragma warning restore WFO3001

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        [MemberNotNull(nameof(_serviceProvider))]
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1006, 721);
            Name = "MainForm";
            Text = "MainForm";
            ResumeLayout(false);
        }

        #endregion
    }
}
