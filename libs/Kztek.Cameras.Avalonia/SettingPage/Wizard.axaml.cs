using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kztek.Cameras
{
    /// <summary>
    /// Wizard — Avalonia <see cref="Window"/> port of the WinForms <c>Wizard</c> Form.
    ///
    /// Key behavioral differences from WinForms:
    /// <list type="bullet">
    ///   <item><see cref="Panel"/> workPanel → <see cref="ContentControl"/> (<c>workContent</c>).
    ///         Changing pages sets <c>workContent.Content</c> instead of Show()/Hide() children.</item>
    ///   <item><c>ShowDialog()</c> + <c>DialogResult</c> → <see cref="ShowWizardAsync"/> using
    ///         <see cref="TaskCompletionSource{T}"/>. Closed-event guard ensures the TCS is
    ///         always resolved even when the user dismisses the window via the X button or Alt+F4.</item>
    /// </list>
    ///
    /// Result semantics:
    /// <list type="bullet">
    ///   <item><c>true</c>  — user clicked "Hoàn thành" (Finish) and all Apply() calls succeeded.</item>
    ///   <item><c>false</c> — user clicked "Hủy bỏ" (Cancel).</item>
    ///   <item><c>null</c>  — user closed the window via X button or Alt+F4 (Closed guard).</item>
    /// </list>
    /// </summary>
    public partial class Wizard : Window
    {
        // ── State ────────────────────────────────────────────────────────────────────────────

        private readonly List<IWizardPage> _pages = new();
        private string _title = string.Empty;
        private int _currentPage;
        private IWizardPage? _currentPageControl;

        /// <summary>
        /// TCS for async result tracking.
        /// Set via <see cref="BtnFinish_Click"/> (true) or <see cref="BtnCancel_Click"/> (false);
        /// the Closed guard always calls <c>TrySetResult(null)</c> as a fallback so the Task
        /// never hangs if the window is dismissed unexpectedly.
        /// </summary>
        private TaskCompletionSource<bool?>? _tcs;

        // ── Constructor ──────────────────────────────────────────────────────────────────────

        public Wizard()
        {
            InitializeComponent();

            // Equivalent to WinForms Wizard_Load — fires after AXAML layout is built
            // and all pages have been added by the caller (before ShowWizardAsync).
            this.Opened += OnWindowOpened;

            // TCS Closed guard — MUST use TrySetResult (not SetResult) to avoid
            // InvalidOperationException when Finish/Cancel already set the TCS result.
            // Covers: X button, Alt+F4, and any programmatic Close() without explicit result.
            this.Closed += (_, _) => _tcs?.TrySetResult(null);
        }

        // ── Public API ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a wizard page. Call before <see cref="ShowWizardAsync"/>.
        /// The page object must implement <see cref="IWizardPage"/> AND derive from
        /// <see cref="Avalonia.Controls.Control"/> (typically <see cref="UserControl"/>).
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="page"/> is not an Avalonia Control.
        /// </exception>
        public void AddPage(IWizardPage page)
        {
            if (page is not Control)
                throw new ArgumentException(
                    "Wizard page must derive from Avalonia.Controls.Control (e.g. UserControl).",
                    nameof(page));

            page.StateChanged += Page_StateChanged;
            page.Reset += Page_Reset;
            _pages.Add(page);
        }

        /// <summary>
        /// Shows the Wizard as a modal dialog owned by <paramref name="owner"/> and returns
        /// the result asynchronously once the window is closed.
        /// </summary>
        /// <returns>
        /// <c>true</c> on Finish, <c>false</c> on Cancel, <c>null</c> on X/Alt+F4.
        /// </returns>
        public async Task<bool?> ShowWizardAsync(Window owner)
        {
            _tcs = new TaskCompletionSource<bool?>();

            // ShowDialog is modal (blocks owner interaction) and completes when the
            // window closes. By the time it returns, the Closed event has already
            // fired and _tcs is guaranteed to be set by either a button handler or
            // the Closed guard above.
            await this.ShowDialog(owner);

            return _tcs.Task.Result;
        }

        // ── Window lifecycle ─────────────────────────────────────────────────────────────────

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            // Save original title (WinForms: title = this.Text in Wizard_Load).
            _title = this.Title ?? "Wizard";

            // Navigate to the first page.
            if (_pages.Count > 0)
                SetCurrentPage(0);
        }

        // ── Navigation ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Navigates to page <paramref name="n"/> (0-based).
        /// Equivalent to WinForms <c>SetCurrentPage(int)</c>.
        /// </summary>
        private void SetCurrentPage(int n)
        {
            OnPageChanging(n);

            _currentPage = n;

            // Update window title: "Wizard - Page 1 of 3"
            this.Title = $"{_title} - Page {n + 1} of {_pages.Count}";

            if (n >= 0 && n < _pages.Count)
            {
                _currentPageControl = _pages[_currentPage];

                // Equivalent to WinForms: show the nth control in workPanel.
                // ContentControl holds exactly one child at a time — no Show()/Hide() needed.
                workContent.Content = (Control)_currentPageControl;

                // Update description banner text.
                descriptionLabel.Text = _currentPageControl.PageDescription;

                // Notify the page it is now displayed.
                _currentPageControl.Display();

                UpdateControlButtons();
            }
        }

        /// <summary>
        /// Updates the enabled state of Back, Next, and Finish buttons.
        /// Behavior parity with WinForms <c>UpdateControlButtons()</c>.
        /// </summary>
        private void UpdateControlButtons()
        {
            // "Quay lại" (Back) — disabled on page 0.
            btnBack.IsEnabled = (_currentPage != 0);

            // "Tiếp tục" (Next) — enabled only when not on last page AND current page is complete.
            btnNext.IsEnabled = (_currentPage < _pages.Count - 1)
                                && (_currentPageControl != null)
                                && _currentPageControl.Completed;

            // "Hoàn thành" (Finish) — enabled only when ALL pages are completed.
            btnFinish.IsEnabled = true;
            foreach (var page in _pages)
            {
                if (!page.Completed)
                {
                    btnFinish.IsEnabled = false;
                    break;
                }
            }
        }

        // ── Button handlers ──────────────────────────────────────────────────────────────────

        private void BtnNext_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPageControl?.Apply() == true)
            {
                if (_currentPage < _pages.Count - 1)
                    SetCurrentPage(_currentPage + 1);
            }
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
                SetCurrentPage(_currentPage - 1);
        }

        private void BtnFinish_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPageControl?.Apply() == true)
            {
                OnFinish();

                // Set TCS to true BEFORE closing.
                // After Close(), Closed guard fires TrySetResult(null) — no-op since TCS already set.
                _tcs?.TrySetResult(true);
                this.Close();
            }
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            _tcs?.TrySetResult(false);
            this.Close();
        }

        // ── Page event handlers ──────────────────────────────────────────────────────────────

        private void Page_StateChanged(object? sender, EventArgs e)
        {
            UpdateControlButtons();
        }

        private void Page_Reset(object? sender, EventArgs e)
        {
            // Find which page index fired the reset (behavior parity: workPanel.Controls.GetChildIndex).
            if (sender is IWizardPage page)
            {
                int idx = _pages.IndexOf(page);
                if (idx >= 0)
                    OnResetOnPage(idx);
            }
            UpdateControlButtons();
        }

        // ── Virtual hooks (override in subclasses, e.g. CameraForm) ─────────────────────────

        /// <summary>
        /// Called just before the active page changes.
        /// WinForms: <c>protected virtual void OnPageChanging(int page)</c>.
        /// </summary>
        protected virtual void OnPageChanging(int page) { }

        /// <summary>
        /// Called when a <see cref="IWizardPage.Reset"/> event fires on the page at
        /// index <paramref name="page"/>.
        /// WinForms: <c>protected virtual void OnResetOnPage(int page)</c>.
        /// </summary>
        protected virtual void OnResetOnPage(int page) { }

        /// <summary>
        /// Called when the user clicks Finish and the current page's <see cref="IWizardPage.Apply"/>
        /// returns <c>true</c>. Override to save data before the window closes.
        /// WinForms: <c>protected virtual void OnFinish()</c>.
        /// </summary>
        protected virtual void OnFinish() { }
    }
}
