import * as state from '../state/store.js';

export function ErrorBar() {
  const errs = state.errors.value;
  if (!errs.length) return null;

  return (
    <div class="error-bar">
      {errs.map(e => (
        <div key={e.id} class={`error-item error-${e.type}`}>
          <span class="error-type">{e.type}</span>
          <span class="error-message">{e.message}</span>
          <button class="error-dismiss" onClick={() => state.clearError(e.id)}>×</button>
        </div>
      ))}
    </div>
  );
}
