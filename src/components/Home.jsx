import { useRef } from 'preact/hooks';
import * as state from '../state/store.js';

export function Home() {
  const fileRef = useRef(null);
  const nameRef = useRef(null);

  const handleCreate = async () => {
    const name = nameRef.current?.value?.trim();
    if (!name) return;
    try {
      await state.newProject(name);
    } catch (err) {
      state.pushError('operational', `Create failed: ${err.message}`);
    }
  };

  const handleOpen = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      await state.openProjectFile(file);
    } catch (err) {
      state.pushError('operational', `Open failed: ${err.message}`);
    }
    e.target.value = '';
  };

  const handleResume = async (projectId) => {
    try {
      await state.openProjectById(projectId);
    } catch (err) {
      state.pushError('operational', `Resume failed: ${err.message}`);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') handleCreate();
  };

  const allProjects = state.projects.value;

  return (
    <div class="home">
      <div class="home-inner">
        <h1 class="home-title">LLAssist</h1>
        <p class="home-subtitle">Research authoring environment</p>

        <div class="home-actions">
          <div class="home-create">
            <input
              ref={nameRef}
              type="text"
              placeholder="Project name"
              class="home-input"
              onKeyDown={handleKeyDown}
            />
            <button class="btn btn-primary" onClick={handleCreate}>
              New Project
            </button>
          </div>

          <div class="home-divider">or</div>

          <button class="btn btn-secondary" onClick={() => fileRef.current?.click()}>
            Open .llassist file
          </button>
          <input
            ref={fileRef}
            type="file"
            accept=".llassist"
            class="hidden"
            onChange={handleOpen}
          />
        </div>

        {allProjects.length > 0 && (
          <div class="home-recent">
            <h2 class="home-recent-title">In browser</h2>
            <p class="home-recent-note">Projects cached in IndexedDB. Save to disk to keep.</p>
            <ul class="home-project-list">
              {allProjects.map(p => (
                <li key={p.id} class="home-project-item">
                  <button class="home-project-btn" onClick={() => handleResume(p.id)}>
                    <span class="home-project-name">{p.name}</span>
                    <span class="home-project-date">
                      {new Date(p.updatedAt).toLocaleDateString()}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}
