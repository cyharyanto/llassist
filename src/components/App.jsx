import { useEffect } from 'preact/hooks';
import * as state from '../state/store.js';
import { startAutoSave, stopAutoSave } from '../storage/autosave.js';
import { Home } from './Home.jsx';
import { Library } from './Library.jsx';
import { ConfigPanel } from './ConfigPanel.jsx';
import { AnalyzeView } from './AnalyzeView.jsx';
import { ReportView } from './ReportView.jsx';
import { Toolbar } from './Toolbar.jsx';
import { ErrorBar } from './ErrorBar.jsx';

export function App() {
  useEffect(() => {
    state.initialize();

    const onKeyDown = (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        state.saveCurrentProject().catch(err => {
          state.pushError('operational', `Save failed: ${err.message}`);
        });
      }
    };
    window.addEventListener('keydown', onKeyDown);
    startAutoSave(() => {});

    return () => {
      window.removeEventListener('keydown', onKeyDown);
      stopAutoSave();
    };
  }, []);

  const v = state.view.value;

  return (
    <div class="app">
      {v !== 'home' && <Toolbar />}
      <ErrorBar />
      <main class="app-main">
        {v === 'home' && <Home />}
        {v === 'library' && <Library />}
        {v === 'config' && <ConfigPanel />}
        {v === 'analyze' && <AnalyzeView />}
        {v === 'report' && <ReportView />}
      </main>
    </div>
  );
}
