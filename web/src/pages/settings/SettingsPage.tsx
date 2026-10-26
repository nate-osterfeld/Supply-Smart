'use client'

import { useState } from 'react'
import { useStorage } from '../../hooks/useStorage'
import { useUsers } from '../../hooks/useUsers'

type IconProps = { className?: string; strokeWidth?: number }
type IconName = 'bell' | 'boxes' | 'chevron-down' | 'clipboard' | 'chart' | 'dashboard' | 'logout' | 'mail' | 'map-pin' | 'menu' | 'network' | 'package' | 'pencil' | 'save' | 'send' | 'settings' | 'shield' | 'sparkles' | 'trash' | 'users' | 'x' | 'check'

function InlineIcon({ name, className, strokeWidth = 1.8 }: IconProps & { name: IconName }) {
  const paths: Record<IconName, React.ReactNode> = {
    bell: (
      <>
        <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
        <path d="M10 21h4" />
      </>
    ),
    boxes: (
      <>
        <path d="m21 8-9 5-9-5 9-5 9 5Z" />
        <path d="M3 8v8l9 5 9-5V8" />
        <path d="M12 13v8" />
      </>
    ),
    'chevron-down': <path d="m6 9 6 6 6-6" />,
    clipboard: (
      <>
        <rect x="5" y="4" width="14" height="17" rx="2" />
        <path d="M9 4V2h6v2M9 9h6M9 13h6M9 17h3" />
      </>
    ),
    chart: (
      <>
        <path d="M4 19V5M4 19h16" />
        <path d="m7 15 3-4 3 2 5-7" />
      </>
    ),
    dashboard: (
      <>
        <rect x="3" y="3" width="7" height="7" rx="1" />
        <rect x="14" y="3" width="7" height="7" rx="1" />
        <rect x="3" y="14" width="7" height="7" rx="1" />
        <rect x="14" y="14" width="7" height="7" rx="1" />
      </>
    ),
    logout: (
      <>
        <path d="M10 17l5-5-5-5M15 12H3" />
        <path d="M14 3h5v18h-5" />
      </>
    ),
    mail: (
      <>
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <path d="m3 7 9 6 9-6" />
      </>
    ),
    'map-pin': (
      <>
        <path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z" />
        <circle cx="12" cy="10" r="2.5" />
      </>
    ),
    menu: (
      <>
        <path d="M4 6h16M4 12h16M4 18h16" />
      </>
    ),
    network: (
      <>
        <rect x="9" y="3" width="6" height="6" rx="1" />
        <rect x="3" y="15" width="6" height="6" rx="1" />
        <rect x="15" y="15" width="6" height="6" rx="1" />
        <path d="M12 9v3M6 15v-3h12v3" />
      </>
    ),
    package: (
      <>
        <path d="m21 8-9 5-9-5 9-5 9 5Z" />
        <path d="M3 8v8l9 5 9-5V8M12 13v8" />
      </>
    ),
    pencil: (
      <>
        <path d="m4 20 4.5-1L19 8.5a2.1 2.1 0 0 0-3-3L5.5 16 4 20Z" />
        <path d="m14.5 7.5 3 3" />
      </>
    ),
    save: (
      <>
        <path d="M5 3h12l3 3v15H4V3h1Z" />
        <path d="M8 3v6h8V3M8 21v-7h8v7" />
      </>
    ),
    send: (
      <>
        <path d="m22 2-7 20-4-9-9-4Z" />
        <path d="M22 2 11 13" />
      </>
    ),
    settings: (
      <path 
        d="M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM19.4 15l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a2 2 0 0 0-3.4 1.4v.2a2 2 0 0 1-4 0v-.2a2 2 0 0 0-3.4-1.4l-.1.1a2 2 0 1 1-2.8-2.8l-.1-.1A2 2 0 0 0 1.7 12a2 2 0 0 1 0-4h.2a2 2 0 0 0 1.4-3.4l-.1-.1A2 2 0 1 1 6 1.7l.1.1A2 2 0 0 0 9.5.4V.2a2 2 0 0 1 4 0v.2a2 2 0 0 0 3.4 1.4l-.1-.1a2 2 0 1 1 2.8 2.8l-.1.1A2 2 0 0 0 21.2 8h.2a2 2 0 0 1 0 4h-.2a2 2 0 0 0-1.8 3Z" 
        transform="translate(0 4) scale(.67)" 
      />
    ),
    shield: (
      <>
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
        <path d="m9 12 2 2 4-4" />
      </>
    ),
    sparkles: (
      <>
        <path d="m12 3-1.2 4.8L6 9l4.8 1.2L12 15l1.2-4.8L18 9l-4.8-1.2L12 3ZM5 16l-.6 2.4L2 19l2.4.6L5 22l.6-2.4L8 19l-2.4-.6L5 16ZM19 14l-.6 2.4L16 17l2.4.6L19 20l.6-2.4L22 17l-2.4-.6L19 14Z" />
      </>
    ),
    trash: (
      <>
        <path d="M4 7h16M10 11v6M14 11v6M6 7l1 14h10l1-14M9 7V4h6v3" />
      </>
    ),
    users: (
      <>
        <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM22 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8" />
      </>
    ),
    x: (
      <>
        <path d="m6 6 12 12M18 6 6 18" />
      </>
    ),
    check: (
      <path d="M20 6 9 17l-5-5" />
    ),
  }
  
  return (
    <svg 
      aria-hidden="true" 
      className={className} 
      fill="none" 
      viewBox="0 0 24 24" 
      stroke="currentColor" 
      strokeWidth={strokeWidth} 
      strokeLinecap="round" 
      strokeLinejoin="round"
    >
      {paths[name]}
    </svg>
  )
}

const ChevronDown = (props: IconProps) => <InlineIcon name="chevron-down" {...props} />
const Mail = (props: IconProps) => <InlineIcon name="mail" {...props} />
const MapPin = (props: IconProps) => <InlineIcon name="map-pin" {...props} />
const Pencil = (props: IconProps) => <InlineIcon name="pencil" {...props} />
const Save = (props: IconProps) => <InlineIcon name="save" {...props} />
const Send = (props: IconProps) => <InlineIcon name="send" {...props} />
const ShieldCheck = (props: IconProps) => <InlineIcon name="shield" {...props} />
const Sparkles = (props: IconProps) => <InlineIcon name="sparkles" {...props} />
const Trash2 = (props: IconProps) => <InlineIcon name="trash" {...props} />
const Users = (props: IconProps) => <InlineIcon name="users" {...props} />

const settingsTabs = [
  { id: 'billing', label: 'SaaS Subscription & Billing', icon: Sparkles },
  { id: 'email', label: 'Low-Stock Email Templates', icon: Mail },
  { id: 'permissions', label: 'Tenant User Permissions', icon: Users },
  { id: 'locations', label: 'Locations & Warehouses', icon: MapPin },
]

const template = `# 🚨 SupplySmart Low Stock Alert: {{item_name}}

**Location:** {{supply_room}}  
**Current Stock:** {{current_quantity}}  
**Minimum Threshold:** {{minimum_threshold}}

Please restock this item at your earliest convenience.`

export type ToastMessage = {
  id: number;
  type: 'success' | 'error';
  title: string;
  message: string;
}

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState('billing')
  const [alertTrigger, setAlertTrigger] = useState('Item Reaches Low-Stock Limit')
  const [templateValue, setTemplateValue] = useState(template)
  const [saved, setSaved] = useState(false)
  const [toasts, setToasts] = useState<ToastMessage[]>([])

  const addToast = (title: string, message: string, type: 'success' | 'error' = 'success') => {
    const id = Date.now()
    setToasts((prev) => [...prev, { id, title, message, type }])
    window.setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id))
    }, 3500)
  }

  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [editingLocation, setEditingLocation] = useState<any | null>(null)
  const [deletingLocation, setDeletingLocation] = useState<any | null>(null)

  const { locations, isLoadingLocations, createLocation, updateLocation, deleteLocation } = useStorage()

  const handleCreateLocation = async (name: string, isPrimary: boolean) => {
    try {
      await createLocation({ name, isPrimary })
      addToast('Location Created', `Successfully added "${name}".`)
    } catch (error) {
      console.error('Failed to create location:', error)
      addToast('Error', 'Failed to create location.', 'error')
    }
  }

  const handleUpdateLocation = async (name: string, isPrimary: boolean) => {
    if (!editingLocation) return
    try {
      await updateLocation({ locationId: editingLocation.id, dto: { name, isPrimary } })
      setEditingLocation(null)
      addToast('Location Updated', `Successfully updated "${name}".`)
    } catch (error) {
      console.error('Failed to update location:', error)
      addToast('Error', 'Failed to update location.', 'error')
    }
  }

  const handleConfirmDelete = async () => {
    if (!deletingLocation) return
    try {
      await deleteLocation(deletingLocation.id)
      addToast('Location Deleted', `Successfully deleted "${deletingLocation.name}".`)
      setDeletingLocation(null)
    } catch (error) {
      console.error('Failed to delete location:', error)
      addToast('Error', 'Failed to delete location.', 'error')
    }
  }

  const saveChanges = () => {
    setSaved(true)
    addToast('Templates Saved', 'Low-stock email templates updated successfully.')
    window.setTimeout(() => setSaved(false), 2200)
  }

  return (
    <div className="min-h-screen bg-[#f7f8fa] text-[#172033] pb-12">
      <header className="flex min-h-[74px] items-center justify-between border-b border-[#e4e8ee] bg-white px-5 sm:px-8 lg:px-10">
        <div>
          <p className="text-[17px] font-bold tracking-[-0.025em]">Organization Settings</p>
          <p className="mt-0.5 text-[11px] text-[#8c97a6]">Manage your workspace configuration and preferences</p>
        </div>
        <div className="hidden items-center gap-2 rounded-lg border border-[#e2e6eb] bg-[#fbfcfd] px-3 py-2 text-[11px] font-medium text-[#687587] sm:flex">
          <ShieldCheck className="size-3.5 text-[#65758e]" />
          <span>Tenant ID: <strong className="font-semibold text-[#344154]">adv-dent-092</strong></span>
          <span className="mx-0.5 h-3.5 w-px bg-[#dfe4ea]" />
          <span>Admin Access</span>
        </div>
      </header>

      <div className="mx-auto flex max-w-[1320px] flex-col gap-8 px-5 py-7 sm:px-8 lg:flex-row lg:gap-10 lg:px-10 lg:py-9">
        <aside className="w-full shrink-0 lg:w-[245px]">
          <p className="mb-3 px-1 text-[10px] font-bold uppercase tracking-[0.14em] text-[#a1aab7]">Settings</p>
          <div className="flex flex-col gap-1">
            {settingsTabs.map(({ id, label, icon: Icon }) => (
              <button 
                key={id} 
                onClick={() => setActiveTab(id)} 
                className={`flex items-start gap-3 rounded-lg px-3 py-3 text-left text-[13px] leading-5 transition-all ${
                  activeTab === id 
                    ? 'bg-white font-semibold text-[#172033] shadow-[0_1px_3px_rgba(23,32,51,0.08)] ring-1 ring-[#e6e9ee]' 
                    : 'font-medium text-[#778395] hover:bg-white hover:text-[#344154]'
                }`}
              >
                <Icon className={`mt-0.5 size-[16px] shrink-0 ${activeTab === id ? 'text-[#172033]' : 'text-[#9ba5b2]'}`} />
                {label}
              </button>
            ))}
          </div>
          <div className="mt-8 hidden rounded-xl border border-[#e4e8ee] bg-white p-4 lg:block">
            <p className="text-[11px] font-semibold text-[#687587]">Need help?</p>
            <p className="mt-1.5 text-[11px] leading-5 text-[#9aa4b2]">Visit the SupplySmart knowledge base for configuration guides.</p>
            <button className="mt-3 text-[11px] font-semibold text-[#536987] hover:underline">Open knowledge base →</button>
          </div>
        </aside>

        <section className="min-w-0 flex-1 pb-12">
          {activeTab === 'billing' && <BillingPanel />}
          {activeTab === 'email' && <EmailPanel alertTrigger={alertTrigger} setAlertTrigger={setAlertTrigger} templateValue={templateValue} setTemplateValue={setTemplateValue} saveChanges={saveChanges} saved={saved} />}
          {activeTab === 'permissions' && <PermissionsPanel addToast={addToast} />}
          {activeTab === 'locations' && (
            <LocationsPanel 
              locations={locations} 
              isLoadingLocations={isLoadingLocations} 
              onAdd={() => setIsAddModalOpen(true)} 
              onEdit={(loc) => setEditingLocation(loc)} 
              onDelete={(loc) => setDeletingLocation(loc)} 
            />
          )}
        </section>
      </div>

      <LocationModal 
        isOpen={isAddModalOpen}
        title="Add New Location"
        onClose={() => setIsAddModalOpen(false)}
        onSubmit={handleCreateLocation}
      />

      {editingLocation && (
        <LocationModal 
          isOpen={!!editingLocation}
          title="Edit Location"
          initialName={editingLocation.name}
          initialIsPrimary={editingLocation.isPrimary}
          onClose={() => setEditingLocation(null)}
          onSubmit={handleUpdateLocation}
        />
      )}

      <DeleteConfirmationModal 
        isOpen={!!deletingLocation}
        location={deletingLocation}
        onClose={() => setDeletingLocation(null)}
        onConfirm={handleConfirmDelete}
      />

      <ToastContainer toasts={toasts} />
    </div>
  )
}

function ToastContainer({ toasts }: { toasts: ToastMessage[] }) {
  if (toasts.length === 0) return null

  return (
    <div className="fixed bottom-6 left-6 z-50 flex flex-col gap-2 pointer-events-none">
      {toasts.map((t) => (
        <div 
          key={t.id} 
          className="pointer-events-auto flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-xl transition-all duration-300 animate-in fade-in slide-in-from-bottom-3 dark:border-slate-800 dark:bg-slate-900"
        >
          <div className={`flex size-8 shrink-0 items-center justify-center rounded-full ${
            t.type === 'success' 
              ? 'bg-emerald-50 text-emerald-600 dark:bg-emerald-950 dark:text-emerald-400' 
              : 'bg-rose-50 text-rose-600 dark:bg-rose-950 dark:text-rose-400'
          }`}>
            <InlineIcon name={t.type === 'success' ? 'check' : 'x'} className="size-4" />
          </div>
          <div>
            <p className="text-xs font-semibold text-slate-900 dark:text-slate-100">{t.title}</p>
            <p className="text-[11px] text-slate-500 dark:text-slate-400">{t.message}</p>
          </div>
        </div>
      ))}
    </div>
  )
}

function LocationModal({ 
  isOpen, 
  title, 
  initialName = '', 
  initialIsPrimary = false, 
  onClose, 
  onSubmit 
}: { 
  isOpen: boolean; 
  title: string; 
  initialName?: string; 
  initialIsPrimary?: boolean; 
  onClose: () => void; 
  onSubmit: (name: string, isPrimary: boolean) => void 
}) {
  const [name, setName] = useState(initialName)
  const [isPrimary, setIsPrimary] = useState(initialIsPrimary)

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 backdrop-blur-xs p-4 animate-in fade-in duration-200">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl border border-slate-100 ring-1 ring-slate-900/5">
        <div className="flex items-center justify-between border-b border-slate-100 pb-4">
          <div>
            <h3 className="text-sm font-bold text-slate-900">{title}</h3>
            <p className="mt-0.5 text-[11px] text-slate-500">Configure your physical store or warehouse branch details.</p>
          </div>
          <button onClick={onClose} className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition-colors">
            <InlineIcon name="x" className="size-4" />
          </button>
        </div>
        
        <div className="mt-5 space-y-4">
          <div>
            <label className="block text-[11px] font-bold uppercase tracking-wider text-slate-600 mb-1.5" htmlFor="loc-name">Location Name</label>
            <input 
              id="loc-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Westside Distribution Center"
              className="h-10 w-full rounded-xl border border-slate-200 bg-slate-50/50 px-3.5 text-xs font-medium text-slate-900 outline-none transition-all focus:border-slate-900 focus:bg-white focus:ring-2 focus:ring-slate-900/10"
              autoFocus
            />
          </div>

          <div className="flex items-center gap-3 rounded-xl border border-slate-100 bg-slate-50/50 p-3.5 transition-colors hover:bg-slate-50">
            <input 
              id="loc-primary"
              type="checkbox"
              checked={isPrimary}
              onChange={(e) => setIsPrimary(e.target.checked)}
              className="size-4 rounded border-slate-300 text-slate-900 focus:ring-slate-900"
            />
            <label className="text-xs font-semibold text-slate-800 select-none cursor-pointer" htmlFor="loc-primary">
              Designate as Primary Location Hub
            </label>
          </div>
        </div>

        <div className="mt-6 flex justify-end gap-2.5 border-t border-slate-100 pt-4">
          <button onClick={onClose} className="h-9 rounded-xl border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 transition-all">
            Cancel
          </button>
          <button 
            onClick={() => {
              if (!name.trim()) return
              onSubmit(name.trim(), isPrimary)
              onClose()
            }}
            className="h-9 rounded-xl bg-slate-900 px-4 text-xs font-semibold text-white shadow-xs hover:bg-slate-800 transition-all active:scale-95"
          >
            Save Location
          </button>
        </div>
      </div>
    </div>
  )
}

function DeleteConfirmationModal({
  isOpen,
  location,
  onClose,
  onConfirm
}: {
  isOpen: boolean;
  location: any | null;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const [typedName, setTypedName] = useState('')

  if (!isOpen || !location) return null

  const isMatch = typedName === location.name

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl border border-red-100 ring-1 ring-red-950/5">
        <div className="flex items-start gap-4">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-red-50 text-red-600 ring-4 ring-red-50/50">
            <Trash2 className="size-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-slate-900">Delete Location</h3>
            <p className="mt-0.5 text-[11px] text-red-600 font-semibold">Warning: This action cannot be undone</p>
          </div>
        </div>

        <div className="mt-4 rounded-xl bg-red-50/60 p-3.5 text-xs text-red-900/90 leading-relaxed border border-red-100/80">
          This will permanently delete <strong className="font-semibold text-red-950">{location.name}</strong>, including all nested rooms, storage bins, and active inventory items housed within it.
        </div>

        <div className="mt-5 space-y-2">
          <label className="block text-xs font-medium text-slate-700" htmlFor="confirm-name">
            Please type <span className="rounded-md bg-slate-100 px-1.5 py-0.5 font-mono font-bold text-slate-900 select-all border border-slate-200">{location.name}</span> to confirm:
          </label>
          <input 
            id="confirm-name"
            type="text"
            value={typedName}
            onChange={(e) => setTypedName(e.target.value)}
            placeholder={location.name}
            className={`h-10 w-full rounded-xl border bg-slate-50/50 px-3.5 font-mono text-xs text-slate-900 outline-none transition-all ${
              isMatch 
                ? 'border-red-500 bg-red-50/20 ring-2 ring-red-500/10' 
                : 'border-slate-200 focus:border-slate-900 focus:bg-white focus:ring-2 focus:ring-slate-900/10'
            }`}
            autoFocus
            autoComplete="off"
          />
        </div>

        <div className="mt-6 flex justify-end gap-2.5 border-t border-slate-100 pt-4">
          <button onClick={() => { setTypedName(''); onClose(); }} className="h-9 rounded-xl border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 transition-all">
            Cancel
          </button>
          <button 
            disabled={!isMatch}
            onClick={() => {
              if (!isMatch) return
              onConfirm()
              setTypedName('')
              onClose()
            }}
            className={`h-9 rounded-xl px-4 text-xs font-semibold text-white transition-all ${
              isMatch 
                ? 'bg-red-600 hover:bg-red-700 shadow-sm shadow-red-600/20 active:scale-95 cursor-pointer' 
                : 'bg-slate-200 text-slate-400 cursor-not-allowed'
            }`}
          >
            Delete this location
          </button>
        </div>
      </div>
    </div>
  )
}

function BillingPanel() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <p className="text-[14px] font-semibold">SaaS Subscription &amp; Billing</p>
        <p className="mt-1 text-[12px] text-[#8994a3]">Review your current plan and manage workspace capacity.</p>
      </div>
      <div className="overflow-hidden rounded-xl border border-[#e1e6ec] bg-white shadow-[0_1px_2px_rgba(23,32,51,0.02)]">
        <div className="flex flex-col gap-5 border-b border-[#edf0f3] p-5 sm:flex-row sm:items-center sm:justify-between sm:p-6">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.1em] text-[#9aa4b2]">Current plan</p>
            <div className="mt-2 flex items-center gap-3">
              <h1 className="text-[19px] font-bold tracking-[-0.03em]">Professional Plan</h1>
              <span className="rounded-md bg-[#e9f1ff] px-2 py-1 text-[10px] font-bold uppercase tracking-[0.08em] text-[#5577a8]">Active</span>
            </div>
            <p className="mt-2 text-[12px] text-[#8b96a5]">Billed annually · Renews on October 12, 2026</p>
          </div>
          <button className="h-9 rounded-lg border border-[#d8dee6] px-3.5 text-[12px] font-semibold text-[#46546a] transition-colors hover:bg-[#f8f9fb]">
            Manage billing
          </button>
        </div>
        <div className="p-5 sm:p-6">
          <div className="flex items-end justify-between">
            <div>
              <p className="text-[13px] font-semibold">Active Supply Rooms</p>
              <p className="mt-1 text-[11px] text-[#98a2b0]">Containers allocation</p>
            </div>
            <p className="text-[13px] font-semibold text-[#344154]">8 <span className="font-normal text-[#9ca5b1]">/ 15 Rooms Used</span></p>
          </div>
          <div className="mt-4 h-2 overflow-hidden rounded-full bg-[#edf0f3]">
            <div className="h-full w-[53.33%] rounded-full bg-[#607697]" />
          </div>
          <p className="mt-2.5 text-[11px] text-[#9aa4b2]">7 rooms remaining on your current plan</p>
        </div>
      </div>
      <div className="flex flex-col gap-3 rounded-xl border border-[#dbe6f6] bg-[#f2f6fc] p-5 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <div>
          <p className="text-[13px] font-semibold text-[#344d70]">Need infinite rooms and custom data exporting?</p>
          <p className="mt-1 text-[11px] text-[#7790b4]">Unlock advanced controls and unlimited workspace capacity.</p>
        </div>
        <button className="flex h-9 shrink-0 items-center justify-center gap-2 rounded-lg bg-[#415a7c] px-4 text-[12px] font-semibold text-white shadow-sm transition-colors hover:bg-[#344d70]">
          Upgrade to Enterprise <ChevronDown className="size-3.5 -rotate-90" />
        </button>
      </div>
    </div>
  )
}

function EmailPanel({ alertTrigger, setAlertTrigger, templateValue, setTemplateValue, saveChanges, saved }: { alertTrigger: string; setAlertTrigger: (value: string) => void; templateValue: string; setTemplateValue: (value: string) => void; saveChanges: () => void; saved: boolean }) {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <p className="text-[14px] font-semibold">Low-Stock Email Templates</p>
        <p className="mt-1 text-[12px] text-[#8994a3]">Customize the markdown text blocks auto-fired to phone networks during threshold triggers.</p>
      </div>
      <div className="rounded-xl border border-[#e1e6ec] bg-white shadow-[0_1px_2px_rgba(23,32,51,0.02)]">
        <div className="border-b border-[#edf0f3] p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-[#f0f3f7] text-[#64748b]">
              <InlineIcon name="bell" className="size-4" />
            </div>
            <div>
              <p className="text-[13px] font-semibold">Alert notification content</p>
              <p className="mt-1 text-[11px] leading-5 text-[#929cab]">Use the available variables to personalize notifications sent to your team.</p>
            </div>
          </div>
          <label className="mt-5 block text-[11px] font-semibold text-[#536174]" htmlFor="trigger">Alert Event Trigger</label>
          <div className="relative mt-2">
            <select 
              id="trigger" 
              value={alertTrigger} 
              onChange={(event) => setAlertTrigger(event.target.value)} 
              className="h-10 w-full appearance-none rounded-lg border border-[#dfe4ea] bg-white px-3 text-[12px] text-[#344154] outline-none transition-shadow focus:border-[#9aabc2] focus:ring-3 focus:ring-[#e7edf5]"
            >
              <option>Item Reaches Low-Stock Limit</option>
              <option>Item Falls Out of Stock</option>
              <option>Weekly Inventory Digest</option>
            </select>
            <ChevronDown className="pointer-events-none absolute right-3 top-3 size-4 text-[#8994a3]" />
          </div>
        </div>
        <div className="p-5 sm:p-6">
          <div className="flex items-center justify-between">
            <label className="text-[11px] font-semibold text-[#536174]" htmlFor="template">Message template</label>
            <span className="text-[10px] text-[#a1aab7]">Markdown supported</span>
          </div>
          <textarea 
            id="template" 
            value={templateValue} 
            onChange={(event) => setTemplateValue(event.target.value)} 
            className="mt-2 min-h-[245px] w-full resize-y rounded-lg border border-[#dfe4ea] bg-[#fbfcfd] p-4 font-mono text-[12px] leading-6 text-[#485568] outline-none transition-shadow focus:border-[#9aabc2] focus:ring-3 focus:ring-[#e7edf5]" 
            spellCheck={false} 
          />
          <div className="mt-5 flex flex-col-reverse gap-3 border-t border-[#edf0f3] pt-5 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-[11px] text-[#9aa4b2]">Last updated by Jordan Davis · Just now</p>
            <div className="flex gap-2">
              <button className="flex h-9 items-center justify-center gap-2 rounded-lg border border-[#d8dee6] px-3.5 text-[12px] font-semibold text-[#536174] transition-colors hover:bg-[#f8f9fb]">
                <Send className="size-3.5" />Send Test Alert
              </button>
              <button 
                onClick={saveChanges} 
                className="flex h-9 items-center justify-center gap-2 rounded-lg bg-[#172033] px-3.5 text-[12px] font-semibold text-white transition-colors hover:bg-[#2b374b]"
              >
                <Save className="size-3.5" />{saved ? 'Changes Saved' : 'Save Changes'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

function LocationsPanel({ locations, isLoadingLocations, onAdd, onEdit, onDelete }: { locations: any[]; isLoadingLocations: boolean; onAdd: () => void; onEdit: (loc: any) => void; onDelete: (loc: any) => void }) {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-[14px] font-semibold">Physical Stores &amp; Warehouses</p>
          <p className="mt-1 text-[12px] text-[#8994a3]">Manage the physical locations connected to your inventory workspace.</p>
        </div>
        <button 
          onClick={onAdd} 
          className="inline-flex h-9 items-center justify-center gap-1.5 rounded-lg bg-[#172033] px-3.5 text-[12px] font-semibold text-white transition-colors hover:bg-[#2b3850]"
        >
          <span className="text-[15px] leading-none">+</span> Add Location
        </button>
      </div>
      <div className="overflow-hidden rounded-xl border border-[#e1e6ec] bg-white shadow-[0_1px_2px_rgba(23,32,51,0.02)]">
        <div className="overflow-x-auto">
          {isLoadingLocations ? (
            <p className="p-8 text-xs text-slate-400">Loading locations...</p>
          ) : locations.length === 0 ? (
            <p className="p-8 text-xs text-slate-400">No physical locations found. Click "+ Add Location" to get started.</p>
          ) : (
            <table className="w-full min-w-[620px] border-collapse text-left">
              <thead>
                <tr className="border-b border-[#edf0f3] bg-[#fbfcfd]">
                  <th className="px-5 py-3.5 text-[10px] font-bold uppercase tracking-[0.1em] text-[#9aa4b2]">Name</th>
                  <th className="px-5 py-3.5 text-[10px] font-bold uppercase tracking-[0.1em] text-[#9aa4b2]">Rooms count</th>
                  <th className="px-5 py-3.5 text-[10px] font-bold uppercase tracking-[0.1em] text-[#9aa4b2]">Status</th>
                  <th className="px-5 py-3.5 text-right text-[10px] font-bold uppercase tracking-[0.1em] text-[#9aa4b2]">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#edf0f3]">
                {locations.map((loc) => (
                  <tr key={loc.id} className="transition-colors hover:bg-[#fcfdfe]">
                    <td className="px-5 py-4 text-[12px] font-semibold text-[#344154]">{loc.name}</td>
                    <td className="px-5 py-4 text-[12px] text-[#778395]">{loc.rooms?.length || 0} rooms</td>
                    <td className="px-5 py-4">
                      <span className={`inline-flex rounded-md px-2.5 py-1 text-[10px] font-semibold ${
                        loc.isPrimary ? 'bg-[#e9f1ff] text-[#5577a8]' : 'bg-[#edf7f1] text-[#4c8064]'
                      }`}>
                        {loc.isPrimary ? 'Primary Hub' : 'Active'}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-right">
                      <div className="flex justify-end gap-2">
                        <button 
                          onClick={() => onEdit(loc)} 
                          className="inline-flex items-center gap-1 rounded-md border border-[#dfe4ea] px-2.5 py-1.5 text-[11px] font-semibold text-[#5d6b7f] hover:bg-[#f8f9fb]" 
                          aria-label={`Edit ${loc.name}`}
                        >
                          <Pencil className="size-3" />Edit
                        </button>
                        <button 
                          onClick={() => onDelete(loc)} 
                          className="inline-flex items-center gap-1 rounded-md border border-[#eadfe0] px-2.5 py-1.5 text-[11px] font-semibold text-[#9a6267] hover:bg-[#fff8f8]" 
                          aria-label={`Delete ${loc.name}`}
                        >
                          <Trash2 className="size-3" />Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}

function InviteUserModal({ 
  isOpen, 
  onClose, 
  onSubmit,
  addToast
}: { 
  isOpen: boolean; 
  onClose: () => void; 
  onSubmit: (dto: { fullName: string; email: string; role: string }) => Promise<any>;
  addToast: (title: string, message: string, type?: 'success' | 'error') => void;
}) {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('Manager');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim() || !email.trim()) return;
    
    try {
      setSubmitting(true);
      await onSubmit({ fullName: fullName.trim(), email: email.trim(), role });
      setFullName('');
      setEmail('');
      setRole('Manager');
      onClose();
      addToast('Invitation Sent', 'An invitation email has been sent successfully.');
    } catch (err) {
      console.error('Failed to invite user:', err);
      addToast('Error', 'Failed to invite user. Email may already be registered.', 'error');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 backdrop-blur-xs p-4">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl border border-slate-100 ring-1 ring-slate-900/5">
        <div className="flex items-center justify-between border-b border-slate-100 pb-4">
          <div>
            <h3 className="text-sm font-bold text-slate-900">Invite Team Member</h3>
            <p className="mt-0.5 text-[11px] text-slate-500">System will auto-generate a secure temporary password.</p>
          </div>
          <button onClick={onClose} className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition-colors">
            <InlineIcon name="x" className="size-4" />
          </button>
        </div>
        
        <form onSubmit={handleSubmit} className="mt-5 space-y-4">
          <div>
            <label className="block text-[11px] font-bold uppercase tracking-wider text-slate-600 mb-1.5" htmlFor="inv-name">Full Name</label>
            <input 
              id="inv-name"
              type="text"
              required
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="e.g. Alex Morgan"
              className="h-10 w-full rounded-xl border border-slate-200 bg-slate-50/50 px-3.5 text-xs font-medium text-slate-900 outline-none focus:border-slate-900 focus:bg-white focus:ring-2 focus:ring-slate-900/10"
            />
          </div>

          <div>
            <label className="block text-[11px] font-bold uppercase tracking-wider text-slate-600 mb-1.5" htmlFor="inv-email">Work Email</label>
            <input 
              id="inv-email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="alex@supplysmart.io"
              className="h-10 w-full rounded-xl border border-slate-200 bg-slate-50/50 px-3.5 text-xs font-medium text-slate-900 outline-none focus:border-slate-900 focus:bg-white focus:ring-2 focus:ring-slate-900/10"
            />
          </div>

          <div>
            <label className="block text-[11px] font-bold uppercase tracking-wider text-slate-600 mb-1.5" htmlFor="inv-role">Assigned Role</label>
            <select 
              id="inv-role"
              value={role}
              onChange={(e) => setRole(e.target.value)}
              className="h-10 w-full rounded-xl border border-slate-200 bg-slate-50/50 px-3.5 text-xs font-medium text-slate-900 outline-none focus:border-slate-900 focus:bg-white focus:ring-2 focus:ring-slate-900/10"
            >
              <option value="Admin">Admin</option>
              <option value="Manager">Manager</option>
              <option value="Staff">Staff</option>
            </select>
          </div>

          <div className="mt-6 flex justify-end gap-2.5 border-t border-slate-100 pt-4">
            <button 
              type="button"
              onClick={onClose}
              className="h-9 rounded-xl border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 transition-all"
            >
              Cancel
            </button>
            <button 
              type="submit"
              disabled={submitting}
              className="h-9 rounded-xl bg-slate-900 px-4 text-xs font-semibold text-white shadow-xs hover:bg-slate-800 transition-all"
            >
              {submitting ? 'Generating Invite...' : 'Send Invite'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function RevokeUserModal({
  isOpen,
  user,
  onClose,
  onConfirm
}: {
  isOpen: boolean;
  user: { id: number; fullName: string; email: string } | null;
  onClose: () => void;
  onConfirm: () => void;
}) {
  if (!isOpen || !user) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl border border-red-100 ring-1 ring-red-950/5">
        <div className="flex items-start gap-4">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-red-50 text-red-600 ring-4 ring-red-50/50">
            <Trash2 className="size-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-slate-900">Revoke Access</h3>
            <p className="mt-0.5 text-[11px] text-red-600 font-semibold">Warning: This user will lose workspace access</p>
          </div>
        </div>

        <div className="mt-4 rounded-xl bg-red-50/60 p-3.5 text-xs text-red-900/90 leading-relaxed border border-red-100/80">
          Are you sure you want to revoke access for <strong className="font-semibold text-red-950">{user.fullName}</strong> ({user.email})? They will immediately be signed out of this tenant.
        </div>

        <div className="mt-6 flex justify-end gap-2.5 border-t border-slate-100 pt-4">
          <button onClick={onClose} className="h-9 rounded-xl border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 transition-all">
            Cancel
          </button>
          <button 
            onClick={() => {
              onConfirm()
              onClose()
            }}
            className="h-9 rounded-xl bg-red-600 px-4 text-xs font-semibold text-white shadow-sm shadow-red-600/20 hover:bg-red-700 transition-all active:scale-95"
          >
            Yes, Revoke Access
          </button>
        </div>
      </div>
    </div>
  )
}

function PermissionsPanel({ addToast }: { addToast: (title: string, message: string, type?: 'success' | 'error') => void }) {
  const { users, isLoadingUsers, inviteUser, deleteUser } = useUsers();
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [revokingUser, setRevokingUser] = useState<{ id: number; fullName: string; email: string } | null>(null);

  const handleConfirmRevoke = async () => {
    if (!revokingUser) return;
    try {
      await deleteUser(revokingUser.id);
      addToast('Access Revoked', `Successfully revoked access for ${revokingUser.fullName}.`);
      setRevokingUser(null);
    } catch (err) {
      console.error('Failed to revoke user:', err);
      addToast('Error', 'Failed to revoke user access.', 'error');
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div>
        <p className="text-[14px] font-semibold">Tenant User Permissions</p>
        <p className="mt-1 text-[12px] text-[#8994a3]">Control who can view, edit, and administer your SupplySmart workspace.</p>
      </div>
      <div className="rounded-xl border border-[#e1e6ec] bg-white p-6 shadow-[0_1px_2px_rgba(23,32,51,0.02)]">
        <div className="flex items-center justify-between border-b border-[#edf0f3] pb-5">
          <div className="flex items-center gap-3">
            <div className="flex size-9 items-center justify-center rounded-lg bg-[#f0f3f7] text-[#64748b]">
              <InlineIcon name="users" className="size-4" />
            </div>
            <div>
              <p className="text-[13px] font-semibold">Workspace roles</p>
              <p className="mt-1 text-[11px] text-[#929cab]">{users.length} members have access to this tenant.</p>
            </div>
          </div>
          <button 
            onClick={() => setIsInviteModalOpen(true)}
            className="inline-flex h-9 items-center justify-center gap-1.5 rounded-lg bg-[#172033] px-3.5 text-[12px] font-semibold text-white transition-colors hover:bg-[#2b3850]"
          >
            + Invite team member
          </button>
        </div>

        {isLoadingUsers ? (
          <p className="py-8 text-xs text-slate-400 text-center">Loading tenant users...</p>
        ) : users.length === 0 ? (
          <p className="py-8 text-xs text-slate-400 text-center">No users found.</p>
        ) : (
          <div className="flex flex-col divide-y divide-[#edf0f3]">
            {users.map((u) => {
              const initials = u.fullName.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
              return (
                <div key={u.id} className="flex items-center gap-3 py-4">
                  <div className="flex size-8 items-center justify-center rounded-full bg-[#e8edf4] text-[10px] font-bold text-[#61718a]">{initials}</div>
                  <div className="min-w-0 flex-1">
                    <p className="text-[12px] font-semibold">{u.fullName}</p>
                    <p className="truncate text-[11px] text-[#99a3b0]">{u.email}</p>
                  </div>
                  <span className="rounded-md bg-[#f2f4f7] px-2.5 py-1 text-[10px] font-semibold text-[#687587]">{u.role}</span>
                  <button 
                    onClick={() => setRevokingUser({ id: u.id, fullName: u.fullName, email: u.email })}
                    className="ml-2 rounded-md border border-slate-200 p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 hover:border-red-200 transition-colors"
                    title="Revoke access"
                  >
                    <Trash2 className="size-3.5" />
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <InviteUserModal 
        isOpen={isInviteModalOpen}
        onClose={() => setIsInviteModalOpen(false)}
        onSubmit={inviteUser}
        addToast={addToast}
      />

      <RevokeUserModal 
        isOpen={!!revokingUser}
        user={revokingUser}
        onClose={() => setRevokingUser(null)}
        onConfirm={handleConfirmRevoke}
      />
    </div>
  );
}