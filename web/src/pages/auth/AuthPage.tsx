import { useState, useEffect, type FormEvent } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { useNavigate } from 'react-router-dom';
import type { AuthResponseDto, LocationDto } from '../../models';

type IconName = 'package' | 'sparkles' | 'eye' | 'eyeOff' | 'arrowRight' | 'lock' | 'mapPin' | 'check' | 'chevronRight' | 'shield';

interface IconProps {
  name: IconName;
  className?: string;
}

function Icon({ name, className = 'size-5' }: IconProps) {
  const common = { 
    className, 
    viewBox: '0 0 24 24', 
    fill: 'none', 
    stroke: 'currentColor', 
    strokeWidth: 1.8, 
    strokeLinecap: 'round' as const, 
    strokeLinejoin: 'round' as const, 
    'aria-hidden': true 
  };
  
  const paths: Record<IconName, React.ReactNode> = {
    package: <><path d="m16.5 9.4-9-5.19" /><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z" /><path d="M3.27 6.96 12 12.01l8.73-5.05M12 22.08V12" /></>,
    sparkles: <><path d="m12 3-1.2 3.8L7 8l3.8 1.2L12 13l1.2-3.8L17 8l-3.8-1.2L12 3ZM19 13l-.7 2.3L16 16l2.3.7L19 19l.7-2.3L22 16l-2.3-.7L19 13ZM5 14l-.8 2.2L2 17l2.2.8L5 20l.8-2.2L8 17l-2.2-.8L5 14Z" /></>,
    eye: <><path d="M2.1 12s3.5-6 9.9-6 9.9 6 9.9 6-3.5 6-9.9 6-9.9-6-9.9-6Z" /><circle cx="12" cy="12" r="2.5" /></>,
    eyeOff: <><path d="m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 5.1A10.7 10.7 0 0 1 12 5c6.4 0 9.9 7 9.9 7a18.2 18.2 0 0 1-3 3.8M6.6 6.7C3.7 8.4 2.1 12 2.1 12s3.5 7 9.9 7c1.1 0 2.1-.2 3-.5" /></>,
    arrowRight: <><path d="M5 12h14M13 6l6 6-6 6" /></>,
    lock: <><rect x="4" y="10" width="16" height="11" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></>,
    mapPin: <><path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z" /><circle cx="12" cy="10" r="2.5" /></>,
    check: <path d="m5 12 4 4L19 6" />,
    chevronRight: <path d="m9 18 6-6-6-6" />,
    shield: <><path d="M12 22s8-3.8 8-10V5l-8-3-8 3v7c0 6.2 8 10 8 10Z" /><path d="m9 12 2 2 4-4" /></>,
  };
  
  return <svg {...common}>{paths[name]}</svg>;
}

export default function AuthPage() {
  const navigate = useNavigate();
  const { login, register } = useAuth();

  useEffect(() => {
    const token = localStorage.getItem('supplysmart_token');
    const locationId = localStorage.getItem('supplysmart_location_id');
    if (token && locationId) {
      navigate('/dashboard', { replace: true });
    }
  }, [navigate]);
  
  const [step, setStep] = useState<'auth' | 'locations'>('auth');
  const [mode, setMode] = useState<'sign-in' | 'create'>('sign-in');
  const [showPassword, setShowPassword] = useState<boolean>(false);

  // Form inputs
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [orgName, setOrgName] = useState<string>('');
  const [adminFullName, setAdminFullName] = useState<string>('');

  // Response session data
  const [authResponse, setAuthResponse] = useState<AuthResponseDto | null>(null);
  const [selectedLocationId, setSelectedLocationId] = useState<number | null>(null);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (mode === 'sign-in') {
      login.mutate({ email, password }, {
        onSuccess: (data: AuthResponseDto) => handleAuthSuccess(data),
        onError: (err: Error) => alert(err.message)
      });
    } else {
      register.mutate({ organizationName: orgName, adminFullName, email, password }, {
        onSuccess: (data: AuthResponseDto) => handleAuthSuccess(data),
        onError: (err: Error) => alert(err.message)
      });
    }
  };

  const handleAuthSuccess = (data: AuthResponseDto) => {
    setAuthResponse(data);
    const primary = data.locations?.find((l: LocationDto) => l.isPrimary) || data.locations?.[0];
    if (primary) setSelectedLocationId(primary.id);
    setStep('locations');
  };

  const handleLaunchWorkspace = () => {
    if (!selectedLocationId) return;
    localStorage.setItem('supplysmart_location_id', selectedLocationId.toString());
    navigate('/dashboard');
  };

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-50 px-6 py-12 font-sans text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <section className="relative flex w-full max-w-[470px] flex-col items-center">
        <div className="mb-8 flex items-center gap-3 text-[22px] font-semibold tracking-[-0.03em] text-slate-900 dark:text-white">
          <span className="flex size-11 items-center justify-center rounded-[14px] bg-blue-600 text-white shadow-lg">
            <Icon name="package" className="size-6" />
          </span>
          SupplySmart
        </div>

        <div className="w-full rounded-[24px] border border-slate-200 bg-white/95 p-8 shadow-xl sm:p-10 dark:border-slate-800 dark:bg-slate-900/90">
          <div className="mb-8 text-center">
            <h1 className="text-[30px] font-semibold tracking-[-0.04em]">
              {step === 'auth' ? 'Welcome back' : 'Select your location'}
            </h1>
            <p className="mx-auto mt-2 max-w-[330px] text-sm text-slate-500 dark:text-slate-400">
              {step === 'auth' ? 'Sign in to manage inventory across your entire business.' : 'Choose the workspace branch you want to open for this session.'}
            </p>
          </div>

          {step === 'auth' ? (
            <>
              <div className="mb-7 grid grid-cols-2 rounded-xl bg-slate-100 p-1 dark:bg-slate-800">
                {(['sign-in', 'create'] as const).map((item) => (
                  <button 
                    key={item} 
                    type="button" 
                    onClick={() => setMode(item)} 
                    className={`rounded-lg px-3 py-2.5 text-xs font-semibold transition ${
                      mode === item ? 'bg-white text-blue-600 shadow-sm dark:bg-slate-900 dark:text-blue-400' : 'text-slate-500'
                    }`}
                  >
                    {item === 'sign-in' ? 'Sign In' : 'Create Organization'}
                  </button>
                ))}
              </div>

              <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
                {mode === 'create' && (
                  <>
                    <label className="flex flex-col gap-2 text-xs font-bold text-slate-700 dark:text-slate-300">
                      Organization name
                      <input 
                        className="h-12 rounded-xl border border-slate-200 bg-white px-4 text-sm dark:border-slate-700 dark:bg-slate-800" 
                        placeholder="Acme Medical" 
                        value={orgName}
                        onChange={(e) => setOrgName(e.target.value)}
                        required 
                      />
                    </label>
                    <label className="flex flex-col gap-2 text-xs font-bold text-slate-700 dark:text-slate-300">
                      Admin Full Name
                      <input 
                        className="h-12 rounded-xl border border-slate-200 bg-white px-4 text-sm dark:border-slate-700 dark:bg-slate-800" 
                        placeholder="Dr. Sarah Jenkins" 
                        value={adminFullName}
                        onChange={(e) => setAdminFullName(e.target.value)}
                        required 
                      />
                    </label>
                  </>
                )}

                <label className="flex flex-col gap-2 text-xs font-bold text-slate-700 dark:text-slate-300">
                  Work email
                  <input 
                    type="email" 
                    className="h-12 rounded-xl border border-slate-200 bg-white px-4 text-sm dark:border-slate-700 dark:bg-slate-800" 
                    placeholder="you@company.com" 
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required 
                  />
                </label>

                <label className="flex flex-col gap-2 text-xs font-bold text-slate-700 dark:text-slate-300">
                  Password
                </label>
                <div className="relative">
                  <input 
                    type={showPassword ? 'text' : 'password'} 
                    className="h-12 w-full rounded-xl border border-slate-200 bg-white px-4 pr-12 text-sm dark:border-slate-700 dark:bg-slate-800" 
                    placeholder="Password" 
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required 
                  />
                  <button 
                    type="button" 
                    className="absolute right-3 top-1/2 -translate-y-1/2 p-2 text-slate-400 hover:text-blue-600" 
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    <Icon name={showPassword ? 'eyeOff' : 'eye'} className="size-5" />
                  </button>
                </div>

                <button 
                  className="mt-1 flex h-12 items-center justify-center gap-2 rounded-xl bg-blue-600 text-sm font-bold text-white transition hover:bg-blue-700 disabled:opacity-50" 
                  type="submit"
                  disabled={login.isPending || register.isPending}
                >
                  {login.isPending || register.isPending ? 'Processing...' : (mode === 'sign-in' ? 'Sign In' : 'Create Organization')}
                </button>
              </form>
            </>
          ) : (
            <div className="flex flex-col gap-5">
              <div className="flex flex-col gap-3">
                {authResponse?.locations?.map((loc: LocationDto) => (
                  <button 
                    key={loc.id} 
                    type="button" 
                    onClick={() => setSelectedLocationId(loc.id)} 
                    className={`flex items-center gap-3 rounded-2xl border p-4 text-left transition ${
                      selectedLocationId === loc.id ? 'border-blue-600 bg-blue-50/70 shadow-sm dark:bg-blue-950/30' : 'border-slate-200 dark:border-slate-700'
                    }`}
                  >
                    <span className="min-w-0 flex-1">
                      <strong className="block truncate text-sm text-slate-900 dark:text-white">{loc.name}</strong>
                      <small className="mt-1 block text-xs text-slate-500">
                        {loc.isPrimary ? 'Primary Facility Hub' : 'Active Branch'}
                      </small>
                    </span>
                  </button>
                ))}
              </div>

              <button 
                className="flex h-12 items-center justify-center gap-2 rounded-xl bg-blue-600 text-sm font-bold text-white transition hover:bg-blue-700" 
                onClick={handleLaunchWorkspace}
              >
                Launch Workspace <Icon name="chevronRight" className="size-4" />
              </button>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}