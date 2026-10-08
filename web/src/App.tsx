import { Outlet } from 'react-router-dom';

export default function App() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      {/* Global layout shells like Header/Sidebar can go here later */}
      <main>
        <Outlet />
      </main>
    </div>
  );
}