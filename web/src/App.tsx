import { useState, useEffect } from 'react';
import { 
  LayoutDashboard, 
  MapPin, 
  ClipboardList, 
  Boxes, 
  BarChart3,
  Settings2, 
  Package,
  ChevronDown,
  Search,
  Bell,
  User
} from 'lucide-react';

import AuthPage from './pages/auth/AuthPage';
import DashboardPage from './pages/dashboard/DashboardPage';
import StoragePage from './pages/storage/StoragePage';
import CatalogPage from './pages/catalog/CatalogPage';
import SettingsPage from './pages/settings/SettingsPage';

export default function App() {
  const [currentScreen, setCurrentScreen] = useState<string>('dashboard');

  useEffect(() => {
    const token = localStorage.getItem('supplysmart_token');
    if (!token) {
      setCurrentScreen('auth');
    }
  }, []);

  const navigateTo = (screenName: string) => {
    setCurrentScreen(screenName);
  };

  const userName = localStorage.getItem('supplysmart_name') || 'Workspace User';
  const userRole = localStorage.getItem('supplysmart_role') || 'Staff';
  
  // Check if current user has admin privileges
  const isAdmin = userRole === 'Admin'

  const workspaceItems = [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { id: 'storage', label: 'Storage Units', icon: MapPin },
    { id: 'forecast', label: 'Schedule Forecast', icon: ClipboardList },
    { id: 'catalog', label: 'Item Catalog', icon: Boxes },
    { id: 'reports', label: 'Reports', icon: BarChart3 },
  ];

  // Restrict manage items strictly to administrators
  const manageItems = isAdmin ? [
    { id: 'settings', label: 'Workspace Settings', icon: Settings2 },
  ] : [];

  if (currentScreen === 'auth' || !localStorage.getItem('supplysmart_token')) {
    return <AuthPage />;
  }

  const initials = userName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .substring(0, 2)
    .toUpperCase();

  return (
    <div className="flex h-screen overflow-hidden bg-[#f7f8fa] font-sans text-slate-900">
      {/* Sidebar */}
      <aside className="flex w-[246px] shrink-0 flex-col border-r border-slate-200/80 bg-white">
        <div className="flex h-[72px] items-center gap-3 border-b border-slate-200/80 px-6">
          <div className="flex size-8 items-center justify-center rounded-lg bg-slate-950 text-white shadow-sm">
            <Package className="size-4" />
          </div>
          <div>
            <p className="text-[15px] font-semibold tracking-tight">SupplySmart</p>
            <p className="text-[10px] font-medium uppercase tracking-[0.16em] text-slate-400">Inventory OS</p>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-4 py-6 space-y-6">
          <div>
            <p className="mb-3 px-3 text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Workspace</p>
            <nav className="flex flex-col gap-1">
              {workspaceItems.map(({ id, label, icon: Icon }) => {
                const active = currentScreen === id;
                return (
                  <button
                    key={id}
                    onClick={() => navigateTo(id)}
                    className={`flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-[13px] font-medium transition-colors ${
                      active ? 'bg-slate-100 text-slate-950 font-semibold' : 'text-slate-500 hover:bg-slate-50 hover:text-slate-900'
                    }`}
                  >
                    <Icon className={`size-4 ${active ? 'text-blue-600' : 'text-slate-400'}`} />
                    <span>{label}</span>
                    {active && <span className="ml-auto size-1.5 rounded-full bg-blue-600" />}
                  </button>
                );
              })}
            </nav>
          </div>

          {manageItems.length > 0 && (
            <div>
              <p className="mb-3 px-3 text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Manage</p>
              <nav className="flex flex-col gap-1">
                {manageItems.map(({ id, label, icon: Icon }) => {
                  const active = currentScreen === id;
                  return (
                    <button
                      key={id}
                      onClick={() => navigateTo(id)}
                      className={`flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-[13px] font-medium transition-colors ${
                        active ? 'bg-slate-100 text-slate-950 font-semibold' : 'text-slate-500 hover:bg-slate-50 hover:text-slate-900'
                      }`}
                    >
                      <Icon className={`size-4 ${active ? 'text-blue-600' : 'text-slate-400'}`} />
                      <span>{label}</span>
                      {active && <span className="ml-auto size-1.5 rounded-full bg-blue-600" />}
                    </button>
                  );
                })}
              </nav>
            </div>
          )}
        </div>

        <div className="border-t border-slate-200/80 p-4">
          <button 
            onClick={() => {
              localStorage.clear();
              navigateTo('auth');
            }}
            className="flex w-full items-center gap-3 rounded-xl px-2 py-2 text-left hover:bg-slate-50 transition-colors group"
            title="Click to sign out"
          >
            <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-blue-100 text-xs font-bold text-blue-700">
              {initials}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-xs font-semibold text-slate-800">{userName}</p>
              <p className="truncate text-[11px] text-slate-400 capitalize">{userRole}</p>
            </div>
            <ChevronDown className="size-4 text-slate-400 transition-transform group-hover:translate-y-0.5" />
          </button>
        </div>
      </aside>

      {/* Right Viewport Area (Header + Main Content) */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Sticky Global Top Header Bar */}
        <header className="sticky top-0 z-45 border-b border-slate-200 bg-white px-6 py-3.5 shadow-xs dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center justify-between gap-4">
            <div className="relative flex w-full max-w-md items-center">
              <Search className="absolute left-3 size-4 text-slate-400" />
              <input 
                aria-label="Search inventory" 
                placeholder="Search inventory, SKUs, or locations..." 
                className="h-9 w-full rounded-md border border-slate-200 bg-slate-100 pl-10 pr-3 text-sm outline-none ring-blue-500 focus:ring-2 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100" 
              />
            </div>
            <div className="flex items-center gap-2">
              <button 
                aria-label="Notifications" 
                className="flex size-9 items-center justify-center rounded-md text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
                title="Notifications"
              >
                <Bell className="size-5" />
              </button>
              <button 
                aria-label="User profile" 
                className="flex size-9 items-center justify-center rounded-md text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
                title={userName}
              >
                <User className="size-5" />
              </button>
            </div>
          </div>
        </header>

        {/* Dynamic Screen Viewport */}
        <main className="flex-1 overflow-y-auto bg-[#f7f8fa] p-8">
          {currentScreen === 'dashboard' && <DashboardPage onNavigate={navigateTo} />}
          {currentScreen === 'storage' && <StoragePage />}
          {currentScreen === 'catalog' && <CatalogPage />}
          {currentScreen === 'settings' && <SettingsPage />}
        </main>
      </div>
    </div>
  );
}