use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct EngineConfig {
    pub is_doh_enabled: bool,
    pub doh_provider_url: String,
    pub block_quic: bool,
    pub max_payload_size: i32,
    pub fragment_http: i32,
    pub fragment_https: i32,
    pub auto_split_sni: bool,
    pub reverse_fragmentation: bool,
    pub buffer_poisoning: bool,
    pub junk_packet_size: i32,
    pub junk_packet_count: u16,
    pub junk_packet_ttl: i32,
    pub junk_packet_bad_checksum: bool,
    pub junk_packet_bad_sequence: bool,
    pub mix_host: bool,
    pub host_no_space: bool,
    pub additional_space: bool,
    pub fake_packet_ttl: i32,
    pub bad_sequence: bool,
    pub bad_check_sum: bool,
    pub fake_request_resend_count: i32,
}

impl Default for EngineConfig {
    fn default() -> Self {
        Self {
            is_doh_enabled: true,
            doh_provider_url: "https://1.1.1.1/dns-query".to_string(),
            block_quic: false,
            max_payload_size: 1200,
            fragment_http: 0,
            fragment_https: 0,
            auto_split_sni: false,
            reverse_fragmentation: false,
            buffer_poisoning: false,
            junk_packet_size: 1,
            junk_packet_count: 1,
            junk_packet_ttl: 5,
            junk_packet_bad_checksum: false,
            junk_packet_bad_sequence: false,
            mix_host: false,
            host_no_space: false,
            additional_space: false,
            fake_packet_ttl: 5,
            bad_sequence: false,
            bad_check_sum: false,
            fake_request_resend_count: 1,
        }
    }
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct ExternalEngine {
    pub id: String,
    pub name: String,
    pub exe_path: String,
    pub script_path: Option<String>,
    pub manual_args: Option<String>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct AppConfig {
    pub active_engine_id: String,
    pub external_engines: Vec<ExternalEngine>,
    pub language: Option<String>,
    pub minimize_to_tray: bool,
}

impl Default for AppConfig {
    fn default() -> Self {
        Self {
            active_engine_id: "internal".to_string(),
            external_engines: Vec::new(),
            language: None,
            minimize_to_tray: true,
        }
    }
}

#[derive(Debug, Serialize)]
#[serde(tag = "code", content = "message")]
pub enum AppError {
    #[serde(rename = "ERR_IO")]
    Io(String),
    #[serde(rename = "ERR_NOT_FOUND")]
    NotFound(String),
    #[serde(rename = "ERR_ENGINE_RUNNING")]
    EngineRunning,
    #[serde(rename = "ERR_CONFIG")]
    Config(String),
    #[serde(rename = "ERR_UNKNOWN")]
    Unknown(String),
}

impl From<String> for AppError {
    fn from(err: String) -> Self {
        AppError::Unknown(err)
    }
}

impl From<&str> for AppError {
    fn from(err: &str) -> Self {
        AppError::Unknown(err.to_string())
    }
}

impl From<std::io::Error> for AppError {
    fn from(err: std::io::Error) -> Self {
        AppError::Io(err.to_string())
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TrayLocales {
    pub start: String,
    pub stop: String,
    pub quit: String,
    pub show: String,
    pub hide: String,
}

impl Default for TrayLocales {
    fn default() -> Self {
        Self {
            start: "Start Engine".to_string(),
            stop: "Stop Engine".to_string(),
            quit: "Quit".to_string(),
            show: "Show".to_string(),
            hide: "Hide".to_string(),
        }
    }
}
