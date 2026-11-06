using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public sealed class ApiResultEventArgs : EventArgs
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string ResponseText { get; private set; }
        public TimeSpan Duration { get; private set; }
        public long ResponseSize { get; private set; }
        public bool ResponseTruncated { get; private set; }
        public ApiResultEventArgs(HttpStatusCode statusCode, string responseText, TimeSpan duration, long responseSize, bool responseTruncated)
        { StatusCode = statusCode; ResponseText = responseText; Duration = duration; ResponseSize = responseSize; ResponseTruncated = responseTruncated; }
    }

    public sealed class ApiErrorEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }
        public string Message { get { return Exception == null ? String.Empty : Exception.Message; } }
        public ApiErrorEventArgs(Exception exception) { Exception = exception; }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Visual workbench for sending and inspecting REST HTTP requests.")]
    public class ZenApiWorkbench : UserControl
    {
        private const int ResponseLimitBytes = 8 * 1024 * 1024;
        private readonly ComboBox _method;
        private readonly TextBox _url;
        private readonly TextBox _headers;
        private readonly TextBox _body;
        private readonly TextBox _response;
        private readonly Button _send;
        private readonly Button _cancel;
        private readonly Button _copy;
        private readonly Label _status;
        private readonly Label _metrics;
        private readonly ToolTip _toolTip;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _activeCts;
        private bool _userCancelRequested;
        private HttpStatusCode? _lastStatusCode;
        private TimeSpan _lastDuration;
        private string _responseText = String.Empty;
        private bool _lastResponseTruncated;
        private bool _disposed;

        public ZenApiWorkbench()
        {
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(460, 420);
            _httpClient = new HttpClient();
            _toolTip = new ToolTip();
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = ZenTheme.Background, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 42)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 58)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(root);
            var requestBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = ZenTheme.Background };
            requestBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104)); requestBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); requestBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82)); requestBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            _method = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, FlatStyle = FlatStyle.Flat };
            _method.Items.AddRange(new object[] { "GET", "POST", "PUT", "PATCH", "DELETE" }); _method.SelectedIndex = 0; _method.AccessibleName = "HTTP Method";
            _url = MakeTextBox(false); _url.AccessibleName = "Request URL"; _url.Text = "https://";
            _send = MakeButton("Send", ZenTheme.Primary); _send.Click += async delegate { await SendAsync(); };
            _cancel = MakeButton("Cancel", ZenTheme.Danger); _cancel.Enabled = false; _cancel.Click += delegate { Cancel(); };
            requestBar.Controls.Add(_method, 0, 0); requestBar.Controls.Add(_url, 1, 0); requestBar.Controls.Add(_send, 2, 0); requestBar.Controls.Add(_cancel, 3, 0); root.Controls.Add(requestBar, 0, 0);
            var requestGroup = MakeGroup("  HEADERS · One line per pair: Name: value");
            var reqLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = ZenTheme.Surface, Padding = new Padding(9) };
            reqLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); reqLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); reqLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); reqLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            reqLayout.Controls.Add(MakeCaption(""), 0, 0);
            _headers = MakeTextBox(true); _headers.AccessibleName = "Headers, one per line"; _headers.AcceptsReturn = true; _headers.ScrollBars = ScrollBars.Vertical; reqLayout.Controls.Add(_headers, 0, 1);
            reqLayout.Controls.Add(MakeCaption("BODY"), 0, 2);
            _body = MakeTextBox(true); _body.AccessibleName = "Request Body"; _body.AcceptsReturn = true; _body.ScrollBars = ScrollBars.Vertical; reqLayout.Controls.Add(_body, 0, 3);
            requestGroup.Controls.Add(reqLayout); root.Controls.Add(requestGroup, 0, 1);
            var responseGroup = MakeGroup("RESPONSE");
            var responseLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = ZenTheme.Surface, Padding = new Padding(9) };
            responseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); responseLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); responseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
            var responseHead = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = ZenTheme.Surface }; responseHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); responseHead.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            _status = MakeCaption("Still no response"); _status.ForeColor = ZenTheme.Accent; _copy = MakeButton("", ZenTheme.SurfaceRaised); _copy.Click += delegate { CopyResponse(); }; responseHead.Controls.Add(_status, 0, 0); responseHead.Controls.Add(_copy, 1, 0); responseLayout.Controls.Add(responseHead, 0, 0);
            _response = MakeTextBox(true); _response.ReadOnly = true; _response.AcceptsReturn = true; _response.ScrollBars = ScrollBars.Both; _response.WordWrap = false; _response.AccessibleName = "Request HTTP text"; responseLayout.Controls.Add(_response, 0, 1);
            _metrics = MakeCaption("Duration: —   Size: —"); responseLayout.Controls.Add(_metrics, 0, 2); responseGroup.Controls.Add(responseLayout); root.Controls.Add(responseGroup, 0, 2);
            var hint = MakeCaption("Only sends after clicking Send. Ctrl+Enter to send · Esc to cancel."); root.Controls.Add(hint, 0, 3);
            _toolTip.SetToolTip(_method, "Select the HTTP method."); _toolTip.SetToolTip(_url, "Absolute URL of the endpoint."); _toolTip.SetToolTip(_headers, "One header per line in the format Name: value. Use SetHeader to set it programmatically."); _toolTip.SetToolTip(_body, "Body sent in POST, PUT or PATCH requests; Specify Content-Type in the headers when necessary."); _toolTip.SetToolTip(_send, "Explicity initiate an HTTP request."); _toolTip.SetToolTip(_cancel, "Cancel active request"); _toolTip.SetToolTip(_copy, "Copy request text.");
        }

        [Category("Request"), Description("HTTP method selected.")]
        public string Method { get { return _method.SelectedItem == null ? "GET" : _method.SelectedItem.ToString(); } set { string v = (value ?? "GET").ToUpperInvariant(); int i = _method.Items.IndexOf(v); if (i < 0) throw new ArgumentException("Unsupported method.", "value"); _method.SelectedIndex = i; } }
        [Category("Request"), Description("Absolute URL to request.")]
        public string Url { get { return _url.Text; } set { _url.Text = value ?? String.Empty; } }
        [Category("Request"), Description("Additionals Headers, one per line on format Name: value.")]
        public string HeadersText { get { return _headers.Text; } set { _headers.Text = value ?? String.Empty; } }
        [Category("Request"), Description("Request body.")]
        public string RequestBody { get { return _body.Text; } set { _body.Text = value ?? String.Empty; } }
        [Browsable(false)] public HttpStatusCode? LastStatusCode { get { return _lastStatusCode; } }
        [Browsable(false)] public TimeSpan LastDuration { get { return _lastDuration; } }
        [Browsable(false)] public string ResponseText { get { return _responseText; } }
        [Browsable(false)] public bool LastResponseTruncated { get { return _lastResponseTruncated; } }
        [Category("Request"), Description("Timeout HTTP; use System.Threading.Timeout.InfiniteTimeSpan to desactive burocratic limit.")]
        public TimeSpan Timeout
        {
            get { return _httpClient.Timeout; }
            set
            {
                if (value != System.Threading.Timeout.InfiniteTimeSpan && value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("value", "Use a positive timeout or a Timeout.InfiniteTimeSpan.");
                _httpClient.Timeout = value;
            }
        }
        public event EventHandler<ApiResultEventArgs> ResultReceived;
        public event EventHandler<ApiErrorEventArgs> RequestError;

        public void SetHeader(string name, string value)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("The header name is obrigatory.", "name");
            string[] lines = _headers.Lines; var updated = new List<string>(); bool found = false;
            foreach (string line in lines) { int colon = line.IndexOf(':'); if (colon > 0 && String.Equals(line.Substring(0, colon).Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) { if (!found && value != null) updated.Add(name.Trim() + ": " + value); found = true; } else if (!String.IsNullOrWhiteSpace(line)) updated.Add(line); }
            if (!found && value != null) updated.Add(name.Trim() + ": " + value);
            _headers.Lines = updated.ToArray();
        }

        public Task SendAsync() { return SendAsync(CancellationToken.None); }
        public async Task SendAsync(CancellationToken cancellationToken)
        {
            if (_disposed) throw new ObjectDisposedException("ZenApiWorkbench");
            Uri uri; if (!Uri.TryCreate(_url.Text.Trim(), UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) { RaiseError(new ArgumentException("Provide a valid absolute URL with an HTTP or HTTPS scheme.")); return; }
            if (_activeCts != null) { RaiseError(new InvalidOperationException("There is a already a request in progress.")); return; }
            HttpMethod method; try { method = new HttpMethod(Method); } catch (Exception ex) { RaiseError(ex); return; }
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); _activeCts = cts; _userCancelRequested = false; _lastResponseTruncated = false; SetBusy(true);
            var timer = Stopwatch.StartNew();
            try
            {
                using (var request = new HttpRequestMessage(method, uri))
                {
                    if (method != HttpMethod.Get && method != HttpMethod.Delete && method != HttpMethod.Head && method != HttpMethod.Options)
                        request.Content = new StringContent(_body.Text, Encoding.UTF8);
                    foreach (string line in _headers.Lines)
                    {
                        if (String.IsNullOrWhiteSpace(line)) continue; int colon = line.IndexOf(':');
                        if (colon <= 0) throw new FormatException("Invalid Header. Use the format Name: value.");
                        string name = line.Substring(0, colon).Trim(), value = line.Substring(colon + 1).Trim();
                        if (!request.Headers.TryAddWithoutValidation(name, value))
                        { if (request.Content == null) request.Content = new StringContent(String.Empty, Encoding.UTF8); if (!request.Content.Headers.TryAddWithoutValidation(name, value)) throw new FormatException("Invalid or not accepted Header: " + name); }
                    }
                    using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                    {
                        byte[] bytes; bool truncated = false;
                        using (Stream body = await response.Content.ReadAsStreamAsync())
                        using (MemoryStream buffer = new MemoryStream())
                        {
                            byte[] chunk = new byte[8192]; int read;
                            while ((read = await body.ReadAsync(chunk, 0, chunk.Length, cts.Token)) > 0)
                            {
                                int remaining = ResponseLimitBytes - (int)buffer.Length;
                                int take = Math.Min(remaining, read);
                                if (take > 0) buffer.Write(chunk, 0, take);
                                if (take < read || buffer.Length >= ResponseLimitBytes)
                                {
                                    truncated = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value > ResponseLimitBytes : true;
                                    break;
                                }
                            }
                            bytes = buffer.ToArray();
                        }
                        string text; try { text = response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.CharSet != null ? Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet.Trim('"')).GetString(bytes) : Encoding.UTF8.GetString(bytes); } catch { text = Encoding.UTF8.GetString(bytes); }
                        if (truncated) text += Environment.NewLine + "[Response limited to 8 MiB to protect the application's memory.]";
                        timer.Stop(); _lastStatusCode = response.StatusCode; _lastDuration = timer.Elapsed; _responseText = text; _lastResponseTruncated = truncated; _response.Text = text;
                        _status.Text = ((int)response.StatusCode) + " " + response.ReasonPhrase; _status.ForeColor = response.IsSuccessStatusCode ? ZenTheme.Accent : ZenTheme.Danger; _metrics.Text = "Duration: " + _lastDuration.TotalMilliseconds.ToString("0") + " ms   Size: " + FormatSize(bytes.LongLength) + (truncated ? " · Limited response" : "");
                        var handler = ResultReceived; if (handler != null) handler(this, new ApiResultEventArgs(response.StatusCode, text, _lastDuration, bytes.LongLength, truncated));
                    }
                }
            }
            catch (OperationCanceledException ex) { if (!_disposed) { if (cancellationToken.IsCancellationRequested || _userCancelRequested) RaiseError(new OperationCanceledException("Request cancelled by the user.", ex, cts.Token)); else RaiseError(new TimeoutException("The response timed out.", ex)); } }
            catch (Exception ex) { if (!_disposed) RaiseError(ex); }
            finally { timer.Stop(); if (Object.ReferenceEquals(_activeCts, cts)) { _activeCts = null; cts.Dispose(); if (!_disposed) SetBusy(false); } }
        }
        public void Cancel() { if (_activeCts != null) { _userCancelRequested = true; _activeCts.Cancel(); } }
        public void CopyResponse() { if (!String.IsNullOrEmpty(_responseText)) try { Clipboard.SetText(_responseText); } catch (Exception ex) { RaiseError(ex); } }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Enter)) { if (_activeCts == null) { Task ignored = SendAsync(); } return true; }
            if (keyData == Keys.Escape && _activeCts != null) { Cancel(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void RaiseError(Exception ex) { _status.Text = "Error: " + ex.Message; _status.ForeColor = ZenTheme.Danger; var handler = RequestError; if (handler != null) handler(this, new ApiErrorEventArgs(ex)); }
        private void SetBusy(bool busy) { _send.Enabled = !busy; _cancel.Enabled = busy; _method.Enabled = !busy; _url.Enabled = !busy; _headers.Enabled = !busy; _body.Enabled = !busy; if (busy) { _status.Text = "Sending…"; _status.ForeColor = ZenTheme.Accent; } }
        private static string FormatSize(long n) { if (n < 1024) return n + " B"; if (n < 1024 * 1024) return (n / 1024.0).ToString("0.0") + " KB"; return (n / (1024.0 * 1024.0)).ToString("0.00") + " MB"; }
        private static TextBox MakeTextBox(bool multiline) { return new TextBox { Dock = DockStyle.Fill, Multiline = multiline, BorderStyle = BorderStyle.FixedSingle, BackColor = ZenTheme.Background, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, AcceptsTab = true }; }
        private static Label MakeCaption(string text) { return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ZenTheme.Muted, BackColor = Color.Transparent, AutoEllipsis = true }; }
        private static Button MakeButton(string text, Color color) { return new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, Cursor = Cursors.Hand, AccessibleName = text }; }
        private static Panel MakeGroup(string title) { var p = new Panel { Dock = DockStyle.Fill, BackColor = ZenTheme.Surface, Padding = new Padding(8), Margin = new Padding(0, 3, 0, 3) }; var l = MakeCaption(title); l.Dock = DockStyle.Top; l.Height = 22; l.ForeColor = ZenTheme.Primary; p.Controls.Add(l); return p; }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed) { _disposed = true; if (_activeCts != null) { try { _activeCts.Cancel(); } catch { } } _httpClient.Dispose(); _toolTip.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake

// Memo: Kono konpōnento wa tesuto-yō ni ofurainde sakusei shimashita. Sakusei-go, tesuto ni 3-nichi kakarimashitaga, saisho wa erā ga dete ita koto ga hanmei shi, sarani 5-nichi kakarimashita. Kono raiburari no naka de mottomo jikan no kakatta konpōnentodesu. Dōzo o tanoshimi kudasa.