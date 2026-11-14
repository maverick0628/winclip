// Performance monitoring tool for WinClip
// Real-time monitoring of CPU, memory, and latency metrics

mod metrics;
mod perf_dashboard;

use metrics::{collect_metrics, monitor_for_duration, LatencyTracker};
use perf_dashboard::{DisplayMode, PerformanceDashboard};
use std::time::Duration;

/// Command-line arguments for the performance monitor
struct Args {
    /// Duration to monitor in seconds (0 = continuous)
    duration: u64,
    /// Sample interval in milliseconds
    interval: u64,
    /// Show continuous updates
    continuous: bool,
    /// Display mode (compact, detailed, json)
    display_mode: DisplayMode,
}

impl Args {
    /// Parses command-line arguments
    fn parse() -> Result<Self, String> {
        let args: Vec<String> = std::env::args().collect();

        // Default values
        let mut duration = 0; // 0 = continuous
        let mut interval = 1000; // 1 second
        let mut continuous = false;
        let mut display_mode = DisplayMode::Detailed;

        let mut i = 1;
        while i < args.len() {
            match args[i].as_str() {
                "--help" | "-h" => {
                    return Err("HELP".to_string());
                }
                "--duration" | "-d" => {
                    i += 1;
                    if i >= args.len() {
                        return Err("--duration requires a value".to_string());
                    }
                    duration = args[i]
                        .parse()
                        .map_err(|_| "Invalid duration value".to_string())?;
                }
                "--interval" | "-i" => {
                    i += 1;
                    if i >= args.len() {
                        return Err("--interval requires a value".to_string());
                    }
                    interval = args[i]
                        .parse()
                        .map_err(|_| "Invalid interval value".to_string())?;
                }
                "--continuous" | "-c" => {
                    continuous = true;
                }
                "--compact" => {
                    display_mode = DisplayMode::Compact;
                }
                "--json" => {
                    display_mode = DisplayMode::Json;
                }
                arg => {
                    return Err(format!("Unknown argument: {}", arg));
                }
            }
            i += 1;
        }

        Ok(Self {
            duration,
            interval,
            continuous,
            display_mode,
        })
    }

    /// Prints help message
    fn print_help() {
        println!("WinClip Performance Monitor");
        println!();
        println!("USAGE:");
        println!("    perf_monitor [OPTIONS]");
        println!();
        println!("OPTIONS:");
        println!("    -h, --help              Print help information");
        println!("    -d, --duration <SECS>   Monitor duration in seconds (default: continuous)");
        println!("    -i, --interval <MS>     Sample interval in milliseconds (default: 1000)");
        println!("    -c, --continuous        Show continuous updates instead of summary");
        println!("    --compact               Use compact display mode (single-line)");
        println!("    --json                  Output in JSON format (for logging/parsing)");
        println!();
        println!("PERFORMANCE TARGETS:");
        println!("    CPU:    < 1% when idle");
        println!("    Memory: < 50MB typical");
        println!("    Latency: < 300ms P95 for popup");
        println!();
        println!("EXAMPLES:");
        println!("    # Monitor for 10 seconds with detailed summary");
        println!("    perf_monitor --duration 10");
        println!();
        println!("    # Continuous monitoring with compact display");
        println!("    perf_monitor --continuous --compact --interval 500");
        println!();
        println!("    # Quick 5-second check with detailed dashboard");
        println!("    perf_monitor -d 5");
        println!();
        println!("    # JSON output for logging/parsing");
        println!("    perf_monitor --json --duration 10");
    }
}

/// Runs the performance monitor
fn run_monitor(args: Args) {
    // Don't print header in JSON mode
    if args.display_mode != DisplayMode::Json {
        println!("WinClip Performance Monitor");
        println!("===========================");
        println!();
    }

    if args.continuous {
        run_continuous_mode(args);
    } else {
        run_summary_mode(args);
    }
}

/// Runs in continuous mode - shows live updates
fn run_continuous_mode(args: Args) {
    let dashboard = PerformanceDashboard::new().with_mode(args.display_mode);

    // Print header (not in JSON mode)
    if args.display_mode != DisplayMode::Json {
        dashboard.print_header();
        println!("Monitoring performance (press Ctrl+C to stop)...");
        println!();
        println!("Target: CPU <1%, Memory <50MB");
        println!();
    }

    let interval = Duration::from_millis(args.interval);
    let duration = if args.duration > 0 {
        Some(Duration::from_secs(args.duration))
    } else {
        None
    };

    let start = std::time::Instant::now();
    let mut sample_count = 0;

    loop {
        // Check if duration has elapsed
        if let Some(max_duration) = duration {
            if start.elapsed() >= max_duration {
                if args.display_mode != DisplayMode::Json {
                    println!();
                    println!("Monitoring complete ({} samples)", sample_count);
                }
                break;
            }
        }

        // Collect and display metrics
        let metrics = collect_metrics();

        // Use dashboard to display metrics
        if args.display_mode == DisplayMode::Compact || args.display_mode == DisplayMode::Json {
            dashboard.log_record(metrics.cpu_usage, metrics.memory_mb());
        } else {
            // For detailed mode, clear screen and redisplay (optional)
            // For now, just print the snapshot
            print!("{}", dashboard.format_snapshot(metrics.cpu_usage, metrics.memory_mb()));
        }

        sample_count += 1;
        std::thread::sleep(interval);
    }

    if args.display_mode != DisplayMode::Json {
        println!();
    }
}

/// Runs in summary mode - shows statistics at the end
fn run_summary_mode(args: Args) {
    let dashboard = PerformanceDashboard::new().with_mode(args.display_mode);

    let duration = if args.duration > 0 {
        args.duration
    } else {
        10 // Default to 10 seconds if not specified
    };

    if args.display_mode != DisplayMode::Json {
        dashboard.print_header();
        println!("Monitoring for {} seconds...", duration);
        println!("Sample interval: {}ms", args.interval);
        println!();
    }

    // Collect samples
    let start = std::time::Instant::now();
    let mut samples = Vec::new();
    let sample_duration = Duration::from_secs(duration);
    let sample_interval = Duration::from_millis(args.interval);

    while start.elapsed() < sample_duration {
        let metrics = collect_metrics();
        samples.push((metrics.cpu_usage, metrics.memory_mb()));
        std::thread::sleep(sample_interval);
    }

    let elapsed = start.elapsed();

    // Display summary using dashboard
    println!("{}", dashboard.format_summary(&samples, elapsed));
}

/// Example: Demonstrates latency tracking
#[allow(dead_code)]
fn example_latency_tracking() {
    println!("Latency Tracking Example");
    println!("========================");
    println!();

    let mut tracker = LatencyTracker::new();

    println!("Simulating 100 popup operations...");

    for _ in 0..100 {
        tracker.start();

        // Simulate popup operation (50-200ms)
        let delay = 50 + (rand() % 150);
        std::thread::sleep(Duration::from_millis(delay));

        tracker.record();
    }

    println!();
    println!("Latency Statistics:");
    println!("  Samples: {}", tracker.sample_count());

    if let Some(avg) = tracker.average() {
        println!("  Average: {:.1}ms", avg.as_secs_f64() * 1000.0);
    }

    if let Some(p95) = tracker.p95() {
        let p95_ms = p95.as_secs_f64() * 1000.0;
        let target_ok = p95_ms < 300.0;

        println!(
            "  {} P95:     {:.1}ms (target: <300ms)",
            if target_ok { "✓" } else { "✗" },
            p95_ms
        );
    }
}

/// Simple pseudo-random number generator for example
fn rand() -> u64 {
    use std::time::SystemTime;
    SystemTime::now()
        .duration_since(SystemTime::UNIX_EPOCH)
        .unwrap()
        .as_nanos() as u64
        % 256
}

fn main() {
    match Args::parse() {
        Ok(args) => run_monitor(args),
        Err(msg) => {
            if msg == "HELP" {
                Args::print_help();
                std::process::exit(0);
            } else {
                eprintln!("Error: {}", msg);
                eprintln!();
                eprintln!("Use --help for usage information");
                std::process::exit(1);
            }
        }
    }
}
