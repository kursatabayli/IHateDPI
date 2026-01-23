export interface AppConfig {
  activeEngineId: string;
  externalEngines: ExternalEngine[];
  language: string | null;
  minimizeToTray: boolean;
}

export interface ExternalEngine {
  id: string;
  name: string;
  exePath: string;
  scriptPath?: string | null;
  manualArgs?: string | null;
}

export interface EngineConfig {
  isDohEnabled: boolean;
  dohProviderUrl: string;
  blockQuic: boolean;
  maxPayloadSize: number;
  fragmentHttp: number;
  fragmentHttps: number;
  autoSplitSni: boolean;
  reverseFragmentation: boolean;
  bufferPoisoning: boolean;
  junkPacketSize: number;
  junkPacketCount: number;
  junkPacketTtl: number;
  junkPacketBadChecksum: boolean;
  junkPacketBadSequence: boolean;
  mixHost: boolean;
  hostNoSpace: boolean;
  additionalSpace: boolean;
  fakePacketTtl: number;
  badSequence: boolean;
  badCheckSum: boolean;
  fakeRequestResendCount: number;
}

export interface AppError {
  code: string;
  message?: string;
}
