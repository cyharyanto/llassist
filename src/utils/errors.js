// Typed failures (spec: Architecture)
// terminal — pass pauses, researcher decides
// retryable — backoff or skip, mark degraded
// informational — surfaced, does not block
// operational — requires intervention

export class AppError extends Error {
  constructor(message, type, details = {}) {
    super(message);
    this.type = type;
    this.details = details;
    this.timestamp = new Date().toISOString();
  }
}

export class TerminalError extends AppError {
  constructor(message, details) { super(message, 'terminal', details); }
}

export class RetryableError extends AppError {
  constructor(message, details) { super(message, 'retryable', details); }
}

export class InformationalError extends AppError {
  constructor(message, details) { super(message, 'informational', details); }
}

export class OperationalError extends AppError {
  constructor(message, details) { super(message, 'operational', details); }
}

export class VersionConflictError extends AppError {
  constructor(storeName, id, expectedVersion, currentEntity) {
    super(
      `Version conflict on ${storeName}/${id}: expected ${expectedVersion}, got ${currentEntity?.version}`,
      'operational',
      { storeName, id, expectedVersion, currentEntity },
    );
    this.currentEntity = currentEntity;
  }
}
