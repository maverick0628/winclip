// Performance dashboard and logger for WinClip
// Provides formatted display of performance metrics with timestamps

use std::time::{Duration, SystemTime, UNIX_EPOCH};

/// Dashboard display mode
#[derive(Debug, Clone, Copy, PartialEq)]
pub enum DisplayMode {
    /// Compact single-line display
    Compact,
    /// Full detailed dashboard
    Detailed,
    /// JSON format for logging/parsing
    Json,
}

/// Performance dashboard for displaying metrics
pub struct PerformanceDashboard {
    /// Display mode
    mode: DisplayMode,
    /// Show timestamps
    show_timestamps: bool,
    /// Start time for relative timestamps
    start_time: SystemTime,
}

impl PerformanceDashboard {
    /// Creates a new dashboard with default settings
    pub fn new() -> Self {
        Self {
            mode: DisplayMode::Detailed,
            show_timestamps: true,
            start_time: SystemTime::now(),
        }
    }

    /// Creates a compact dashboard (single-line output)
    pub fn compact() -> Self {
        Self {
            mode: DisplayMode::Compact,
            show_timestamps: true,
            start_time: SystemTime::now(),
        }
    }

    /// Creates a JSON dashboard (for logging/parsing)
    pub fn json() -> Self {
        Self {
            mode: DisplayMode::Json,
            show_timestamps: true,
            start_time: SystemTime::now(),
        }
    }

    /// Sets the display mode
    pub fn with_mode(mut self, mode: DisplayMode) -> Self {
        self.mode = mode;
        self
    }

    /// Sets whether to show timestamps
    pub fn with_timestamps(mut self, show: bool) -> Self {
        self.show_timestamps = show;
        self
    }

    /// Formats a snapshot of performance metrics
    pub fn format_snapshot(&self, cpu_usage: f64, memory_mb: f64) -> String {
        match self.mode {
            DisplayMode::Compact => self.format_compact(cpu_usage, memory_mb),
            DisplayMode::Detailed => self.format_detailed(cpu_usage, memory_mb),
            DisplayMode::Json => self.format_json(cpu_usage, memory_mb),
        }
    }

    /// Formats metrics in compact mode (single line)
    fn format_compact(&self, cpu_usage: f64, memory_mb: f64) -> String {
        let timestamp = if self.show_timestamps {
            format!("[{}] ", self.format_timestamp())
        } else {
            String::new()
        };

        let cpu_status = if cpu_usage < 1.0 { "✓" } else { "✗" };
        let mem_status = if memory_mb < 50.0 { "✓" } else { "✗" };

        format!(
            "{}{} CPU: {:.2}% | {} MEM: {:.2}MB",
            timestamp, cpu_status, cpu_usage, mem_status, memory_mb
        )
    }

    /// Formats metrics in detailed mode (multi-line dashboard)
    fn format_detailed(&self, cpu_usage: f64, memory_mb: f64) -> String {
        let mut output = String::new();

        // Header with timestamp
        if self.show_timestamps {
            output.push_str(&format!("┌─ Performance Snapshot @ {}\n", self.format_timestamp()));
        } else {
            output.push_str("┌─ Performance Snapshot\n");
        }

        // CPU metrics
        let cpu_bar = self.create_bar(cpu_usage, 1.0, 20);
        let cpu_status = if cpu_usage < 1.0 { "✓ OK" } else { "✗ HIGH" };
        output.push_str(&format!(
            "│  CPU:    {:.2}% {} [{}] (target: <1%)\n",
            cpu_usage, cpu_status, cpu_bar
        ));

        // Memory metrics
        let mem_bar = self.create_bar(memory_mb, 50.0, 20);
        let mem_status = if memory_mb < 50.0 { "✓ OK" } else { "✗ HIGH" };
        output.push_str(&format!(
            "│  Memory: {:.2}MB {} [{}] (target: <50MB)\n",
            memory_mb, mem_status, mem_bar
        ));

        // Overall status
        let overall_ok = cpu_usage < 1.0 && memory_mb < 50.0;
        let overall_status = if overall_ok {
            "✓ All targets met"
        } else {
            "✗ Some targets exceeded"
        };
        output.push_str(&format!("│\n│  Status: {}\n", overall_status));

        output.push_str("└────────────────────────────────────────────\n");

        output
    }

    /// Formats metrics in JSON mode
    fn format_json(&self, cpu_usage: f64, memory_mb: f64) -> String {
        let timestamp = self.format_timestamp_unix();
        let cpu_ok = cpu_usage < 1.0;
        let mem_ok = memory_mb < 50.0;

        format!(
            r#"{{"timestamp":{},"timestamp_iso":"{}","cpu_usage":{:.2},"memory_mb":{:.2},"cpu_ok":{},"memory_ok":{},"all_ok":{}}}"#,
            timestamp,
            self.format_timestamp(),
            cpu_usage,
            memory_mb,
            cpu_ok,
            mem_ok,
            cpu_ok && mem_ok
        )
    }

    /// Creates a visual bar chart
    fn create_bar(&self, value: f64, target: f64, width: usize) -> String {
        let percentage = (value / target).min(1.0);
        let filled = (percentage * width as f64) as usize;
        let empty = width.saturating_sub(filled);

        let bar_char = if value < target { "█" } else { "▓" };
        let empty_char = "░";

        format!("{}{}", bar_char.repeat(filled), empty_char.repeat(empty))
    }

    /// Formats the current timestamp as a relative time
    fn format_timestamp(&self) -> String {
        let elapsed = SystemTime::now()
            .duration_since(self.start_time)
            .unwrap_or(Duration::from_secs(0));

        format_duration(elapsed)
    }

    /// Formats the current timestamp as Unix time (seconds since epoch)
    fn format_timestamp_unix(&self) -> u64 {
        SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap_or(Duration::from_secs(0))
            .as_secs()
    }

    /// Logs a performance record
    pub fn log_record(&self, cpu_usage: f64, memory_mb: f64) {
        println!("{}", self.format_snapshot(cpu_usage, memory_mb));
    }

    /// Creates a header for the dashboard
    pub fn print_header(&self) {
        match self.mode {
            DisplayMode::Compact => {
                println!("WinClip Performance Monitor - Compact Mode");
                println!("Targets: CPU <1%, Memory <50MB");
                println!();
            }
            DisplayMode::Detailed => {
                println!("╔════════════════════════════════════════════╗");
                println!("║  WinClip Performance Monitor               ║");
                println!("║  ────────────────────────────────          ║");
                println!("║  Targets: CPU <1% | Memory <50MB           ║");
                println!("╚════════════════════════════════════════════╝");
                println!();
            }
            DisplayMode::Json => {
                // JSON mode doesn't need a header (for easy parsing)
            }
        }
    }

    /// Creates a summary report from multiple samples
    pub fn format_summary(&self, samples: &[(f64, f64)], duration: Duration) -> String {
        if samples.is_empty() {
            return "No samples collected".to_string();
        }

        let cpu_values: Vec<f64> = samples.iter().map(|(cpu, _)| *cpu).collect();
        let mem_values: Vec<f64> = samples.iter().map(|(_, mem)| *mem).collect();

        let avg_cpu = cpu_values.iter().sum::<f64>() / cpu_values.len() as f64;
        let max_cpu = cpu_values
            .iter()
            .max_by(|a, b| a.partial_cmp(b).unwrap())
            .copied()
            .unwrap_or(0.0);
        let min_cpu = cpu_values
            .iter()
            .min_by(|a, b| a.partial_cmp(b).unwrap())
            .copied()
            .unwrap_or(0.0);

        let avg_mem = mem_values.iter().sum::<f64>() / mem_values.len() as f64;
        let max_mem = mem_values
            .iter()
            .max_by(|a, b| a.partial_cmp(b).unwrap())
            .copied()
            .unwrap_or(0.0);
        let min_mem = mem_values
            .iter()
            .min_by(|a, b| a.partial_cmp(b).unwrap())
            .copied()
            .unwrap_or(0.0);

        let cpu_ok = avg_cpu < 1.0 && max_cpu < 2.0;
        let mem_ok = avg_mem < 50.0 && max_mem < 60.0;

        match self.mode {
            DisplayMode::Json => {
                format!(
                    r#"{{"duration_secs":{:.1},"samples":{},"cpu":{{"avg":{:.2},"max":{:.2},"min":{:.2},"ok":{}}},"memory":{{"avg":{:.2},"max":{:.2},"min":{:.2},"ok":{}}},"all_ok":{}}}"#,
                    duration.as_secs_f64(),
                    samples.len(),
                    avg_cpu,
                    max_cpu,
                    min_cpu,
                    cpu_ok,
                    avg_mem,
                    max_mem,
                    min_mem,
                    mem_ok,
                    cpu_ok && mem_ok
                )
            }
            _ => {
                let mut output = String::new();
                output.push_str("╔════════════════════════════════════════════╗\n");
                output.push_str("║  Performance Summary                       ║\n");
                output.push_str("╚════════════════════════════════════════════╝\n\n");
                output.push_str(&format!(
                    "Duration:     {}\n",
                    format_duration(duration)
                ));
                output.push_str(&format!("Samples:      {}\n\n", samples.len()));

                let cpu_status = if cpu_ok { "✓" } else { "✗" };
                output.push_str(&format!("{} CPU Usage:\n", cpu_status));
                output.push_str(&format!("  Average:    {:.2}% (target: <1%)\n", avg_cpu));
                output.push_str(&format!("  Maximum:    {:.2}%\n", max_cpu));
                output.push_str(&format!("  Minimum:    {:.2}%\n\n", min_cpu));

                let mem_status = if mem_ok { "✓" } else { "✗" };
                output.push_str(&format!("{} Memory Usage:\n", mem_status));
                output.push_str(&format!("  Average:    {:.2}MB (target: <50MB)\n", avg_mem));
                output.push_str(&format!("  Maximum:    {:.2}MB\n", max_mem));
                output.push_str(&format!("  Minimum:    {:.2}MB\n\n", min_mem));

                if cpu_ok && mem_ok {
                    output.push_str("✓ All performance targets met!\n");
                } else {
                    output.push_str("✗ Some performance targets not met\n");
                }

                output
            }
        }
    }
}

impl Default for PerformanceDashboard {
    fn default() -> Self {
        Self::new()
    }
}

/// Formats a duration in human-readable form
fn format_duration(duration: Duration) -> String {
    let total_secs = duration.as_secs();
    let millis = duration.subsec_millis();

    if total_secs >= 3600 {
        // Hours
        let hours = total_secs / 3600;
        let mins = (total_secs % 3600) / 60;
        let secs = total_secs % 60;
        format!("{:02}:{:02}:{:02}", hours, mins, secs)
    } else if total_secs >= 60 {
        // Minutes
        let mins = total_secs / 60;
        let secs = total_secs % 60;
        format!("{:02}:{:02}.{:03}", mins, secs, millis)
    } else {
        // Seconds
        format!("{:02}.{:03}s", total_secs, millis)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_dashboard_creation() {
        let dashboard = PerformanceDashboard::new();
        assert_eq!(dashboard.mode, DisplayMode::Detailed);
        assert!(dashboard.show_timestamps);
    }

    #[test]
    fn test_compact_dashboard() {
        let dashboard = PerformanceDashboard::compact();
        assert_eq!(dashboard.mode, DisplayMode::Compact);
    }

    #[test]
    fn test_json_dashboard() {
        let dashboard = PerformanceDashboard::json();
        assert_eq!(dashboard.mode, DisplayMode::Json);
    }

    #[test]
    fn test_format_compact() {
        let dashboard = PerformanceDashboard::compact().with_timestamps(false);
        let output = dashboard.format_snapshot(0.5, 30.0);
        assert!(output.contains("✓ CPU"));
        assert!(output.contains("✓ MEM"));
        assert!(output.contains("0.50%"));
        assert!(output.contains("30.00MB"));
    }

    #[test]
    fn test_format_json() {
        let dashboard = PerformanceDashboard::json();
        let output = dashboard.format_snapshot(0.8, 45.0);
        assert!(output.starts_with("{"));
        assert!(output.ends_with("}"));
        assert!(output.contains("\"cpu_usage\":0.80"));
        assert!(output.contains("\"memory_mb\":45.00"));
        assert!(output.contains("\"cpu_ok\":true"));
        assert!(output.contains("\"memory_ok\":true"));
    }

    #[test]
    fn test_format_detailed() {
        let dashboard = PerformanceDashboard::new().with_timestamps(false);
        let output = dashboard.format_snapshot(0.7, 40.0);
        assert!(output.contains("Performance Snapshot"));
        assert!(output.contains("CPU:"));
        assert!(output.contains("Memory:"));
        assert!(output.contains("✓ All targets met"));
    }

    #[test]
    fn test_target_exceeded() {
        let dashboard = PerformanceDashboard::compact().with_timestamps(false);
        let output = dashboard.format_snapshot(1.5, 60.0);
        assert!(output.contains("✗ CPU"));
        assert!(output.contains("✗ MEM"));
    }

    #[test]
    fn test_create_bar() {
        let dashboard = PerformanceDashboard::new();

        // 50% filled
        let bar = dashboard.create_bar(0.5, 1.0, 10);
        assert_eq!(bar.len(), 10 * "█".len()); // Unicode characters

        // 100% filled
        let bar = dashboard.create_bar(1.0, 1.0, 10);
        assert!(bar.starts_with("█"));

        // Over 100% (should cap at 100%)
        let bar = dashboard.create_bar(2.0, 1.0, 10);
        assert!(bar.starts_with("▓")); // Different character when over target
    }

    #[test]
    fn test_format_summary() {
        let dashboard = PerformanceDashboard::new();
        let samples = vec![(0.5, 30.0), (0.7, 35.0), (0.6, 32.0)];
        let duration = Duration::from_secs(10);

        let summary = dashboard.format_summary(&samples, duration);
        assert!(summary.contains("Performance Summary"));
        assert!(summary.contains("3")); // sample count
        assert!(summary.contains("0.60%")); // average CPU
        assert!(summary.contains("32.33MB")); // average memory
    }

    #[test]
    fn test_format_summary_json() {
        let dashboard = PerformanceDashboard::json();
        let samples = vec![(0.5, 30.0), (0.7, 35.0)];
        let duration = Duration::from_secs(5);

        let summary = dashboard.format_summary(&samples, duration);
        assert!(summary.starts_with("{"));
        assert!(summary.contains("\"duration_secs\":5"));
        assert!(summary.contains("\"samples\":2"));
    }

    #[test]
    fn test_format_duration() {
        assert_eq!(format_duration(Duration::from_millis(500)), "00.500s");
        assert_eq!(format_duration(Duration::from_secs(30)), "30.000s");
        assert_eq!(
            format_duration(Duration::from_secs(90)),
            "01:30.000"
        );
        assert_eq!(
            format_duration(Duration::from_secs(3661)),
            "01:01:01"
        );
    }

    #[test]
    fn test_empty_summary() {
        let dashboard = PerformanceDashboard::new();
        let samples = vec![];
        let duration = Duration::from_secs(0);

        let summary = dashboard.format_summary(&samples, duration);
        assert!(summary.contains("No samples"));
    }
}
