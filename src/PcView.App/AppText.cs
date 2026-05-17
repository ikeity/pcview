using System.Globalization;

namespace PcView.App;

internal static class AppText
{
    private static readonly Dictionary<string, string> ZhCn = new()
    {
        ["App.Subtitle"] = "本地软件观察工具",
        ["Action.Scan"] = "立即扫描",
        ["Action.Rebuild"] = "重建索引",
        ["Action.OpenFolder"] = "打开目录",
        ["Action.LocateExecutable"] = "定位主程序",
        ["Action.RunUninstall"] = "运行官方卸载入口",
        ["Nav.Views"] = "视图",
        ["Nav.Overview"] = "概览",
        ["Nav.Review"] = "建议复查",
        ["Nav.All"] = "全部应用",
        ["Nav.Folders"] = "目录发现",
        ["Nav.Unknown"] = "证据未知",
        ["Safety.Title"] = "安全边界",
        ["Safety.Body"] = "PcView 不删除文件，不清理注册表，也不会静默卸载软件。",
        ["Page.Title"] = "软件库存",
        ["Page.Subtitle.Default"] = "扫描卸载列表、快捷方式、常见软件目录和本地证据。",
        ["Page.Subtitle.NoScan"] = "运行扫描后显示此视图。",
        ["Page.Subtitle.All"] = "显示全部扫描结果。",
        ["Page.Subtitle.Filtered"] = "显示 {0} / {1} 个应用。",
        ["Search.Label"] = "搜索",
        ["Search.Hint"] = "名称、发布者、路径、exe",
        ["Metric.Apps"] = "应用",
        ["Metric.Review"] = "复查",
        ["Metric.Folders"] = "目录",
        ["Metric.Duration"] = "耗时",
        ["Overview.ReviewFocus"] = "复查重点",
        ["Overview.ReviewFocus.Empty"] = "运行扫描后生成复查队列。",
        ["Overview.ReviewFocus.None"] = "没有发现需要复查的应用。如需检查完整库存，可以浏览全部应用。",
        ["Overview.ReviewFocus.Some"] = "{0} 个应用需要复查。建议先查看目录发现和发布者未知的软件。",
        ["Overview.EvidenceHealth"] = "证据质量",
        ["Overview.EvidenceHealth.Empty"] = "扫描后会显示运行证据的可信度。",
        ["Overview.EvidenceHealth.Value"] = "{0} 个应用有中等可信运行证据，{1} 个依赖低可信文件访问证据，{2} 个缺少运行证据。",
        ["Overview.Workflow"] = "建议流程",
        ["Overview.Workflow.1"] = "1. 扫描或重建本地索引。",
        ["Overview.Workflow.2"] = "2. 优先复查目录发现和发布者未知的软件。",
        ["Overview.Workflow.3"] = "3. 先打开目录或定位程序，再决定是否运行卸载入口。",
        ["Overview.SafeBoundary"] = "操作边界",
        ["Overview.SafeBoundary.Body"] = "PcView 不移除文件、不清理注册表、不静默卸载。不确定性会展示为证据质量，而不是恐吓式风险提示。",
        ["Column.Name"] = "名称",
        ["Column.Publisher"] = "发布者",
        ["Column.Source"] = "来源",
        ["Column.Evidence"] = "证据",
        ["Column.Reason"] = "原因",
        ["Inspector.Title"] = "详情",
        ["Inspector.EmptyName"] = "选择一个应用",
        ["Inspector.EmptyMeta"] = "扫描后选择列表项，查看证据和安全操作。",
        ["Inspector.Evidence"] = "证据",
        ["Inspector.Location"] = "位置",
        ["Inspector.Reasons"] = "复查原因",
        ["Inspector.Executables"] = "程序文件",
        ["Inspector.Actions"] = "操作",
        ["Inspector.NoLocation"] = "没有可定位的安装目录。",
        ["Status.Ready"] = "准备就绪。",
        ["Status.Scanning"] = "正在扫描并复用本地缓存...",
        ["Status.Rebuilding"] = "正在重建本地索引...",
        ["Status.Completed"] = "完成。生成时间：{0:G}。",
        ["Status.ScanFailed"] = "扫描失败：{0}",
        ["Dialog.ScanFailed.Title"] = "扫描失败",
        ["Dialog.Uninstall.Title"] = "确认运行卸载入口",
        ["Dialog.Uninstall.Body"] = "PcView 将运行该软件在 Windows 中登记的官方卸载入口：\n\n{0}",
        ["Tray.Open"] = "打开 PcView",
        ["Tray.Scan"] = "扫描",
        ["Tray.Exit"] = "退出",
        ["Source.Folder"] = "目录",
        ["Source.Shortcut"] = "快捷方式",
        ["Source.UninstallList"] = "卸载列表",
        ["Publisher.Unknown"] = "发布者未知",
        ["Evidence.Unknown"] = "未知",
        ["Confidence.Low"] = "低",
        ["Confidence.Medium"] = "中",
        ["Confidence.High"] = "高",
        ["Confidence.Unknown"] = "未知",
        ["Reason.NotInUninstallList"] = "不在卸载列表",
        ["Reason.UninstallResidue"] = "疑似残留卸载项",
        ["Reason.UnknownPublisher"] = "发布者未知",
        ["Reason.UserProfile"] = "安装在用户目录",
        ["Reason.NoLastRun"] = "运行证据不可用",
        ["Reason.Clear"] = "近期或来源清晰",
        ["Reason.Stale"] = "{0} 天没有运行证据"
    };

    private static readonly Dictionary<string, string> EnUs = new()
    {
        ["App.Subtitle"] = "Local software inventory",
        ["Action.Scan"] = "Scan now",
        ["Action.Rebuild"] = "Rebuild index",
        ["Action.OpenFolder"] = "Open folder",
        ["Action.LocateExecutable"] = "Locate executable",
        ["Action.RunUninstall"] = "Run official uninstall entry",
        ["Nav.Views"] = "VIEWS",
        ["Nav.Overview"] = "Overview",
        ["Nav.Review"] = "Review queue",
        ["Nav.All"] = "All apps",
        ["Nav.Folders"] = "Folder discoveries",
        ["Nav.Unknown"] = "Unknown evidence",
        ["Safety.Title"] = "Safe by design",
        ["Safety.Body"] = "PcView does not delete files, clean registry keys, or run silent uninstallers.",
        ["Page.Title"] = "Software inventory",
        ["Page.Subtitle.Default"] = "Scan registered apps, shortcuts, common app folders, and local evidence.",
        ["Page.Subtitle.NoScan"] = "Run a scan to populate this view.",
        ["Page.Subtitle.All"] = "Showing all scanned applications.",
        ["Page.Subtitle.Filtered"] = "Showing {0} of {1} applications.",
        ["Search.Label"] = "Search",
        ["Search.Hint"] = "name, publisher, path, exe",
        ["Metric.Apps"] = "Apps",
        ["Metric.Review"] = "Review",
        ["Metric.Folders"] = "Folders",
        ["Metric.Duration"] = "Duration",
        ["Overview.ReviewFocus"] = "Review focus",
        ["Overview.ReviewFocus.Empty"] = "Run a scan to build the review queue.",
        ["Overview.ReviewFocus.None"] = "No review-worthy applications were found. Browse all apps if you want to inspect the full inventory.",
        ["Overview.ReviewFocus.Some"] = "{0} applications need review. Start with folder discoveries and unknown publishers.",
        ["Overview.EvidenceHealth"] = "Evidence health",
        ["Overview.EvidenceHealth.Empty"] = "Evidence confidence will appear after scanning.",
        ["Overview.EvidenceHealth.Value"] = "{0} apps have medium-confidence run evidence, {1} rely on low-confidence file access evidence, and {2} have no run evidence.",
        ["Overview.Workflow"] = "Suggested workflow",
        ["Overview.Workflow.1"] = "1. Scan or rebuild the local index.",
        ["Overview.Workflow.2"] = "2. Review folder discoveries and unknown publishers first.",
        ["Overview.Workflow.3"] = "3. Open folders or locate executables before running uninstall entries.",
        ["Overview.SafeBoundary"] = "Safe action boundary",
        ["Overview.SafeBoundary.Body"] = "PcView does not remove files, clean registry keys, or run silent uninstallers. Uncertainty is shown as evidence quality, not as alarm language.",
        ["Column.Name"] = "Name",
        ["Column.Publisher"] = "Publisher",
        ["Column.Source"] = "Source",
        ["Column.Evidence"] = "Evidence",
        ["Column.Reason"] = "Reason",
        ["Inspector.Title"] = "Inspector",
        ["Inspector.EmptyName"] = "Select an app",
        ["Inspector.EmptyMeta"] = "Run a scan, then choose an item to inspect evidence and actions.",
        ["Inspector.Evidence"] = "Evidence",
        ["Inspector.Location"] = "Location",
        ["Inspector.Reasons"] = "Review reasons",
        ["Inspector.Executables"] = "Executables",
        ["Inspector.Actions"] = "Actions",
        ["Inspector.NoLocation"] = "No install directory is available.",
        ["Status.Ready"] = "Ready.",
        ["Status.Scanning"] = "Scanning and reusing the local cache...",
        ["Status.Rebuilding"] = "Rebuilding the local index...",
        ["Status.Completed"] = "Completed. Generated at {0:G}.",
        ["Status.ScanFailed"] = "Scan failed: {0}",
        ["Dialog.ScanFailed.Title"] = "Scan failed",
        ["Dialog.Uninstall.Title"] = "Confirm uninstall entry",
        ["Dialog.Uninstall.Body"] = "PcView will run the official uninstall entry registered in Windows:\n\n{0}",
        ["Tray.Open"] = "Open PcView",
        ["Tray.Scan"] = "Scan",
        ["Tray.Exit"] = "Exit",
        ["Source.Folder"] = "Folder",
        ["Source.Shortcut"] = "Shortcut",
        ["Source.UninstallList"] = "Uninstall list",
        ["Publisher.Unknown"] = "Unknown publisher",
        ["Evidence.Unknown"] = "Unknown",
        ["Confidence.Low"] = "low",
        ["Confidence.Medium"] = "medium",
        ["Confidence.High"] = "high",
        ["Confidence.Unknown"] = "unknown",
        ["Reason.NotInUninstallList"] = "Not in uninstall list",
        ["Reason.UninstallResidue"] = "Potential uninstall entry residue",
        ["Reason.UnknownPublisher"] = "Unknown publisher",
        ["Reason.UserProfile"] = "Installed under user profile",
        ["Reason.NoLastRun"] = "Last-run evidence unavailable",
        ["Reason.Clear"] = "Recent or clearly registered",
        ["Reason.Stale"] = "{0} days without run evidence"
    };

    public static CultureInfo CurrentCulture { get; private set; } = ResolveCulture();

    public static string Get(string key)
    {
        Dictionary<string, string> map = IsChinese(CurrentCulture) ? ZhCn : EnUs;
        return map.TryGetValue(key, out string? value) || EnUs.TryGetValue(key, out value)
            ? value
            : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(CurrentCulture, Get(key), args);
    }

    private static CultureInfo ResolveCulture()
    {
        CultureInfo culture = CultureInfo.CurrentUICulture;
        return IsChinese(culture) ? culture : new CultureInfo("zh-CN");
    }

    private static bool IsChinese(CultureInfo culture)
    {
        return culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
    }
}
