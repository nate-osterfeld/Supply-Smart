import { useMemo, useState, useRef, useEffect, type ReactNode } from 'react'
import { useCatalog } from '../../hooks/useCatalog'
import { useStorage } from '../../hooks/useStorage'

type IconName = 'grid' | 'boxes' | 'file' | 'package' | 'clipboard' | 'settings' | 'chevron' | 'menu' | 'plus' | 'search' | 'sliders' | 'shield' | 'arrow' | 'more' | 'clock' | 'archive' | 'pencil'

function Icon({ name, size = 17 }: { name: IconName; size?: number }) {
  const paths: Record<IconName, ReactNode> = {
    grid: (<><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></>),
    boxes: (<><path d="m12 3 8 4.5v9L12 21l-8-4.5v-9L12 3Z" /><path d="m4.5 7.75 7.5 4.5 7.5-4.5M12 12.25V21" /></>),
    file: (<><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" /><path d="M14 2v6h6M8 13h8M8 17h5" /></>),
    package: (<><path d="m12 3 8 4.5v9L12 21l-8-4.5v-9L12 3Z" /><path d="M4.5 7.75 12 12l7.5-4.25M12 12v9" /></>),
    clipboard: (<><rect x="5" y="4" width="14" height="17" rx="2" /><path d="M9 4V3h6v1M9 12l2 2 4-4M9 17h6" /></>),
    settings: (<><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-1.7 1.7-.06-.06a1.7 1.7 0 0 0-1.88-.34 1.7 1.7 0 0 0-1.03 1.56V20h-2.4v-.2a1.7 1.7 0 0 0-1.03-1.56 1.7 1.7 0 0 0-1.88.34l-.06.06-1.7-1.7.06-.06A1.7 1.7 0 0 0 8.4 15a1.7 1.7 0 0 0-1.56-1.03H6v-2.4h.2A1.7 1.7 0 0 0 7.76 10a1.7 1.7 0 0 0-.34-1.88l-.06-.06 1.7-1.7.06-.06A1.7 1.7 0 0 0 11 6.76 1.7 1.7 0 0 0 12.03 5.2V5h2.4v.2A1.7 1.7 0 0 0 15.46 6.76a1.7 1.7 0 0 0 1.88-.34l.06-.06 1.7 1.7-.06.06A1.7 1.7 0 0 0 18.7 10a1.7 1.7 0 0 0 1.56 1.03h.2v2.4h-.2A1.7 1.7 0 0 0 19.4 15Z" /></>),
    chevron: <path d="m6 9 6 6 6-6" />,
    menu: (<><path d="M4 6h16M4 12h16M4 18h16" /></>),
    plus: (<><path d="M12 5v14M5 12h14" /></>),
    search: (<><circle cx="11" cy="11" r="7" /><path d="m20 20-4-4" /></>),
    sliders: (<><path d="M4 6h6M14 6h6M4 12h3M11 12h9M4 18h8M16 18h4" /><circle cx="12" cy="6" r="2" /><circle cx="9" cy="12" r="2" /><circle cx="14" cy="18" r="2" /></>),
    shield: (<><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" /><path d="m9 12 2 2 4-4" /></>),
    arrow: (<><path d="M7 17 17 7M8 7h9v9" /></>),
    more: (<><circle cx="5" cy="12" r="1" /><circle cx="12" cy="12" r="1" /><circle cx="19" cy="12" r="1" /></>),
    clock: (<><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>),
    archive: (<><path d="M4 7h16M6 7v12h12V7M9 11h6M5 4h14v3H5z" /></>),
    pencil: (<><path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" /><path d="m15 5 4 4" /></>),
  }
  return <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>
}

function Status({ value }: { value: string }) {
  const getBadgeStyles = (status: string) => {
    switch (status) {
      case 'In Stock':
        return 'bg-emerald-50 text-emerald-700 ring-emerald-600/25';
      case 'Low Stock':
        return 'bg-amber-50 text-amber-700 ring-amber-600/25';
      case 'Expiring Soon':
        return 'bg-orange-50 text-orange-700 ring-orange-500/30';
      case 'Expired':
        return 'bg-red-50 text-red-700 font-semibold ring-red-600/40';
      default:
        return 'bg-slate-50 text-slate-700 ring-slate-600/25';
    }
  };

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${getBadgeStyles(value)}`}>
      <span className="size-1.5 rounded-full bg-current" />
      {value}
    </span>
  );
}

export default function CatalogPage() {
  const [tab, setTab] = useState<'catalog' | 'audit'>('catalog')
  const [query, setQuery] = useState('')
  const [category, setCategory] = useState('All categories')
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([])
  
  const [isStatusDropdownOpen, setIsStatusDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Edit Item Modal States
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [editingItemId, setEditingItemId] = useState<number | null>(null)
  const [itemName, setItemName] = useState('')
  const [itemSku, setItemSku] = useState('')
  const [itemLot, setItemLot] = useState('')
  const [itemExp, setItemExp] = useState('')
  const [itemQty, setItemQty] = useState(10)
  const [itemMinThreshold, setItemMinThreshold] = useState<number | ''>(2)
  const [itemCost, setItemCost] = useState<number | ''>(5.00)
  const [itemType, setItemType] = useState<number>(0)
  const [itemCategory, setItemCategory] = useState('')

  const [notice, setNotice] = useState('')

  const { catalogItems = [], auditLogs = [], isLoadingCatalog, isLoadingAudits } = useCatalog()
  const { updateItem, deleteItem } = useStorage()

  // Dynamically extract unique categories from loaded catalog items
  const availableCategories = useMemo(() => {
    const categories = catalogItems
      .map((item: any) => item.category)
      .filter((cat: string) => Boolean(cat && cat.trim() !== ''))
    return Array.from(new Set(categories)).sort()
  }, [catalogItems])

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsStatusDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const toggleStatus = (statusValue: string) => {
    setSelectedStatuses(prev => 
      prev.includes(statusValue) 
        ? prev.filter(s => s !== statusValue) 
        : [...prev, statusValue]
    )
  }

  const handleOpenEditModal = (item: any) => {
    setEditingItemId(item.id)
    setItemName(item.name || '')
    setItemSku(item.sku || '')
    setItemCategory(item.category || 'General')
    setItemLot(item.lotNumber || 'LOT-DEFAULT')
    
    let formattedDate = ''
    if (item.expirationDate && item.expirationDate !== 'N/A') {
      const parsedDate = new Date(item.expirationDate)
      if (!isNaN(parsedDate.getTime())) {
        formattedDate = parsedDate.toISOString().split('T')[0]
      }
    }
    setItemExp(formattedDate)
    
    setItemQty(item.quantity ?? 10)
    setItemMinThreshold(item.minimumThreshold ?? 2)
    setItemCost(item.unitCost ?? 5.00)
    setItemType(item.type ?? 0)
    setIsEditModalOpen(true)
  }

  const handleSaveEditedItem = async () => {
    if (editingItemId === null || !itemName.trim()) return
    try {
      const payload = {
        name: itemName,
        sku: itemSku || 'SKU-GEN-01',
        category: itemCategory || 'General',
        lotNumber: itemLot || 'LOT-DEFAULT',
        quantity: Number(itemQty),
        globalThreshold: 50,
        minimumThreshold: itemMinThreshold === '' ? 5 : Number(itemMinThreshold),
        unitCost: itemCost === '' ? 0 : Number(itemCost),
        type: Number(itemType),
        expirationDate: Number(itemType) === 0 && itemExp ? new Date(itemExp).toISOString() : null
      }

      if (updateItem) {
        await updateItem({ itemId: editingItemId, dto: payload })
      }
      setIsEditModalOpen(false)
      notify(`Successfully updated ${itemName}`)
    } catch (error) {
      console.error('Failed to update item:', error)
      notify('Failed to update item')
    }
  }

  const handleDeleteItem = async (itemId: number, itemName: string) => {
    if (window.confirm(`Are you sure you want to delete "${itemName}" from the catalog?`)) {
      try {
        await deleteItem(itemId)
        notify(`Successfully deleted ${itemName}`)
      } catch (error) {
        console.error('Failed to delete item:', error)
        notify('Failed to delete item')
      }
    }
  }

  const filtered = useMemo(
    () => 
      catalogItems.filter(
        (item: any) => 
          (`${item.name} ${item.sku}`).toLowerCase().includes(query.toLowerCase()) && 
          (category === 'All categories' || item.category === category) && 
          (selectedStatuses.length === 0 || selectedStatuses.includes(item.status))
      ), 
    [catalogItems, query, category, selectedStatuses]
  )

  const notify = (msg: string) => { setNotice(msg); window.setTimeout(() => setNotice(''), 2200) }

  return (
    <div className="min-h-screen bg-[#f7f8fa] text-slate-900 pb-12">
      <header className="flex min-h-[72px] items-center justify-between gap-4 border-b border-slate-200/90 bg-white px-5 py-4 sm:px-8">
        <div>
          <p className="text-[11px] font-medium text-slate-400">Catalog / Inventory</p>
          <h1 className="text-[17px] font-semibold tracking-[-0.02em]">Item Catalog &amp; Compliance Audit Trail</h1>
        </div>
        <button onClick={() => notify('New item form ready')} className="inline-flex h-9 shrink-0 items-center gap-2 rounded-lg bg-blue-600 px-3.5 text-xs font-semibold text-white shadow-sm hover:bg-blue-700">
          <Icon name="plus" size={15} /> Add New Item
        </button>
      </header>

      <main className="mx-auto max-w-[1480px] px-5 py-7 sm:px-8 lg:px-10">
        <div className="mb-7 flex flex-col gap-4 border-b border-slate-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-[22px] font-semibold tracking-[-0.03em] text-slate-950">Catalog governance</h2>
            <p className="mt-1 text-[13px] text-slate-500">Manage inventory records and maintain an immutable compliance history.</p>
          </div>
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="text-emerald-600"><Icon name="shield" size={16} /></span> All records compliant <span className="text-slate-300">·</span> Live DB synced
          </div>
        </div>

        <div className="mb-6 flex items-center gap-1 border-b border-slate-200">
          <button onClick={() => setTab('catalog')} className={`relative px-4 pb-3 text-[13px] font-semibold ${tab === 'catalog' ? 'text-blue-700' : 'text-slate-500 hover:text-slate-800'}`}>
            Master Inventory Catalog
            {tab === 'catalog' && <span className="absolute inset-x-0 bottom-[-1px] h-0.5 rounded-full bg-blue-600" />}
          </button>
          <button onClick={() => setTab('audit')} className={`relative px-4 pb-3 text-[13px] font-semibold ${tab === 'audit' ? 'text-blue-700' : 'text-slate-500 hover:text-slate-800'}`}>
            Immutable Audit Ledger
            {tab === 'audit' && <span className="absolute inset-x-0 bottom-[-1px] h-0.5 rounded-full bg-blue-600" />}
          </button>
        </div>

        {tab === 'catalog' ? (
          <section aria-label="Master Inventory Catalog">
            <div className="mb-4 grid gap-3 rounded-xl border border-slate-200 bg-white p-3 shadow-sm sm:grid-cols-[minmax(220px,1fr)_200px_220px_auto]">
              <label className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"><Icon name="search" size={15} /></span>
                <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search by item name or SKU..." className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-xs outline-none focus:border-blue-400 focus:bg-white" />
              </label>
              
              {/* Dynamically Populated Category Dropdown */}
              <select value={category} onChange={(e) => setCategory(e.target.value)} className="h-9 rounded-lg border border-slate-200 bg-white px-3 text-xs text-slate-600 outline-none">
                <option value="All categories">All categories</option>
                {availableCategories.map((cat: any) => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>

              <div className="relative" ref={dropdownRef}>
                <button
                  type="button"
                  onClick={() => setIsStatusDropdownOpen(!isStatusDropdownOpen)}
                  className="flex h-9 w-full items-center justify-between rounded-lg border border-slate-200 bg-white px-3 text-xs text-slate-600 outline-none hover:bg-slate-50"
                >
                  <span>
                    {selectedStatuses.length === 0 
                      ? 'All statuses' 
                      : `${selectedStatuses.length} status${selectedStatuses.length > 1 ? 'es' : ''} selected`}
                  </span>
                  <Icon name="chevron" size={14} />
                </button>

                {isStatusDropdownOpen && (
                  <div className="absolute left-0 top-10 z-20 w-full rounded-xl border border-slate-200 bg-white p-2 shadow-lg">
                    <div className="mb-2 border-b border-slate-100 pb-1.5 px-2 flex items-center justify-between">
                      <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">Filter by Status</span>
                      {selectedStatuses.length > 0 && (
                        <button onClick={() => setSelectedStatuses([])} className="text-[11px] text-blue-600 hover:underline">Clear</button>
                      )}
                    </div>
                    {['In Stock', 'Low Stock', 'Expiring Soon', 'Expired'].map((statusOption) => {
                      const checked = selectedStatuses.includes(statusOption)
                      return (
                        <label key={statusOption} className="flex cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 text-xs text-slate-700 hover:bg-slate-50">
                          <input
                            type="checkbox"
                            checked={checked}
                            onChange={() => toggleStatus(statusOption)}
                            className="size-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                          />
                          {statusOption}
                        </label>
                      )
                    })}
                  </div>
                )}
              </div>
              
              <button onClick={() => { setQuery(''); setCategory('All categories'); setSelectedStatuses([]); }} className="inline-flex h-9 items-center justify-center gap-2 rounded-lg border border-slate-200 px-3 text-xs font-medium text-slate-600 hover:bg-slate-50">
                <Icon name="sliders" size={14} /> Reset
              </button>
            </div>

            <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
              <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
                <div>
                  <h3 className="text-sm font-semibold">Master inventory catalog</h3>
                  <p className="mt-0.5 text-xs text-slate-400">{filtered.length} of {catalogItems.length} records</p>
                </div>
                <button onClick={() => notify('Export queued')} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50">
                  <Icon name="archive" size={14} /> Export
                </button>
              </div>

              <div className="overflow-x-auto">
                {isLoadingCatalog ? (
                  <p className="p-8 text-xs text-slate-400">Loading catalog items...</p>
                ) : (
                  <table className="w-full min-w-[900px] text-left">
                    <thead className="bg-slate-50/80 text-[10px] font-semibold uppercase tracking-[0.12em] text-slate-400">
                      <tr>
                        {['Item name & SKU', 'Category', 'Current stock qty', 'Location', 'Expiration date', 'Status', ''].map((head) => (
                          <th key={head} className="px-5 py-3 font-semibold">{head}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {filtered.map((item: any) => (
                        <tr key={item.id} className="transition-colors hover:bg-slate-50/70">
                          <td className="px-5 py-4">
                            <div className="text-xs font-semibold text-slate-800">{item.name}</div>
                            <div className="mt-1 font-mono text-[10px] text-slate-400">{item.sku}</div>
                          </td>
                          <td className="px-5 py-4 text-xs text-slate-600">{item.category}</td>
                          <td className="px-5 py-4 text-xs font-semibold tabular-nums text-slate-800">{item.quantity}</td>
                          <td className="px-5 py-4 text-xs text-slate-600">{item.location}</td>
                          <td className="px-5 py-4 text-xs text-slate-600">{item.expirationDate}</td>
                          <td className="px-5 py-4"><Status value={item.status} /></td>
                          <td className="px-5 py-4 text-right flex items-center justify-end gap-1">
                            <button onClick={() => handleOpenEditModal(item)} className="rounded-md px-2.5 py-1.5 text-[11px] font-medium text-slate-600 hover:bg-slate-100 border border-slate-200 shadow-2xs">Edit</button>
                            <button onClick={() => handleDeleteItem(item.id, item.name)} className="rounded-md px-2.5 py-1.5 text-[11px] font-medium text-rose-600 hover:bg-rose-50 border border-slate-200 shadow-2xs">Delete</button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          </section>
        ) : (
          <section aria-label="Immutable Audit Ledger">
            <div className="mb-4 flex items-center gap-3 rounded-xl border border-blue-100 bg-blue-50/60 px-4 py-3 text-xs text-blue-800">
              <span className="text-blue-600"><Icon name="shield" size={16} /></span>
              <span><strong>Immutable audit events</strong> — Every catalog mutation is securely logged for compliance review.</span>
            </div>

            <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
              <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
                <h3 className="text-sm font-semibold">Compliance activity ledger</h3>
                <span className="inline-flex items-center gap-2 text-xs font-medium text-emerald-700"><Icon name="shield" size={14} />Chain verified</span>
              </div>

              <div className="overflow-x-auto">
                {isLoadingAudits ? (
                  <p className="p-8 text-xs text-slate-400">Loading audit ledger...</p>
                ) : (
                  <table className="w-full min-w-[850px] text-left">
                    <thead className="bg-slate-50/80 text-[10px] font-semibold uppercase tracking-[0.12em] text-slate-400">
                      <tr>
                        {['Timestamp', 'User', 'Action type', 'Item details', ''].map((head) => (
                          <th key={head} className="px-5 py-3 font-semibold">{head}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {auditLogs.map((log: any) => (
                        <tr key={log.id} className="hover:bg-slate-50/70">
                          <td className="px-5 py-4 text-xs tabular-nums text-slate-500">
                            {new Date(log.timestamp).toLocaleString()}
                          </td>
                          <td className="px-5 py-4 font-medium text-xs text-slate-700">{log.userEmail}</td>
                          <td className="px-5 py-4">
                            <span className="rounded-full bg-blue-50 px-2.5 py-1 text-[11px] font-medium text-blue-700">
                              {log.actionType}
                            </span>
                          </td>
                          <td className="px-5 py-4">
                            <div className="text-xs font-medium text-slate-700">{log.itemDetails}</div>
                            <div className="mt-1 text-[11px] text-slate-400">{log.locationContext}</div>
                          </td>
                          <td className="px-5 py-4 text-right text-slate-300"><Icon name="arrow" size={15} /></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          </section>
        )}
      </main>

      {/* Edit Item Modal */}
      {isEditModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-2xs">
          <div className="w-full max-w-lg rounded-xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="text-lg font-bold text-slate-900">Edit Inventory Item</h3>
            <p className="mt-1 text-xs text-slate-500">Modify stock details, quantities, and compliance thresholds.</p>
            <div className="mt-4 grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className="block text-xs font-medium text-slate-700">Item Name</label>
                <input value={itemName} onChange={e => setItemName(e.target.value)} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-slate-700">Category</label>
                <input 
                  value={itemCategory} 
                  onChange={e => setItemCategory(e.target.value)} 
                  placeholder="e.g., Medical Supplies, PPE & Safety" 
                  className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" 
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">SKU</label>
                <input value={itemSku} onChange={e => setItemSku(e.target.value)} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">Lot Number</label>
                <input value={itemLot} onChange={e => setItemLot(e.target.value)} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">Item Type</label>
                <select value={itemType} onChange={e => setItemType(Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none bg-white">
                  <option value={0}>Consumable (Single-use)</option>
                  <option value={1}>Reusable (Asset/Tray)</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">Quantity</label>
                <input type="number" value={itemQty} onChange={e => setItemQty(Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">Unit Cost ($)</label>
                <input type="number" step="0.01" value={itemCost} onChange={e => setItemCost(e.target.value === '' ? '' : Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">Minimum Threshold</label>
                <input type="number" value={itemMinThreshold} onChange={e => setItemMinThreshold(e.target.value === '' ? '' : Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
              </div>
              {Number(itemType) === 0 && (
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-slate-700">Expiration Date</label>
                  <input type="date" value={itemExp} onChange={e => setItemExp(e.target.value)} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500" />
                </div>
              )}
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <button onClick={() => setIsEditModalOpen(false)} className="h-9 rounded-md border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50">Cancel</button>
              <button onClick={handleSaveEditedItem} className="h-9 rounded-md bg-blue-600 px-4 text-xs font-semibold text-white hover:bg-blue-700">Save Changes</button>
            </div>
          </div>
        </div>
      )}

      {notice && (
        <div role="status" className="fixed bottom-5 left-1/2 z-30 -translate-x-1/2 rounded-lg bg-slate-900 px-4 py-2.5 text-xs font-medium text-white shadow-xl">
          {notice}
        </div>
      )}
    </div>
  )
}