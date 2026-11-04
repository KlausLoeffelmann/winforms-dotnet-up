using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using WarpToolkit.ComponentModel;
using WarpToolkit.WinForms.Extensions.UI;

namespace DotnetUp.WinFormsApp
{
    /// <summary>
    ///  Represents the main window of the application.
    /// </summary>
    public partial class MainForm : Form, IServiceProvider
    {
        private readonly IUserSettingsService _userSettingsService = null!;

        // The service provider is initialized in the constructor
        // that takes an IServiceProvider parameter. Note: This serviceProvider
        // is not available during InitializeComponent.
        private readonly IServiceProvider _serviceProvider;

        private static readonly string SettingsKey_MainFormBounds
            = nameof(SettingsKey_MainFormBounds);

        /// <summary>
        ///  Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        public MainForm(IServiceProvider serviceProvider)
            : this()
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = new DeferredServiceProvider(serviceProvider);
            _userSettingsService = serviceProvider.GetRequiredService<IUserSettingsService>();
        }

        /// <inheritdoc/>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Bounds = _userSettingsService.Get(
                key: SettingsKey_MainFormBounds,
                defaultValue: this.CenterToScreen(
                    horizontalFillGrade: 70,
                    verticalFillGrade: 70));
        }

        /// <inheritdoc/>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            _userSettingsService.Set(
                key: SettingsKey_MainFormBounds,
                value: Bounds);

            _userSettingsService.Flush();
        }
    }

    public static class FormExtensions
    {
        static readonly Dictionary<WeakReference<Form>, IServiceProvider> _serviceProviders = [];

        extension(Form form)
        {
            // Register a service provider for a form
            public void RegisterServiceProvider(IServiceProvider serviceProvider)
            {
                ArgumentNullException.ThrowIfNull(serviceProvider);
                WeakReference<Form> formReference = new(form);

                _serviceProviders[formReference] = serviceProvider;
            }

            // Retrieve the service provider for a form
            public IServiceProvider? GetServiceProvider()
            {
                WeakReference<Form> formReference = new(form);
                if (_serviceProviders.TryGetValue(formReference, out var serviceProvider))
                {
                    return serviceProvider;
                }

                return null;
            }
        }
    }
}
