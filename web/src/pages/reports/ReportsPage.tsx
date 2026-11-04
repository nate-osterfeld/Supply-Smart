'use client'

import { useMemo, useState } from 'react'

type IconName = 
  | 'dashboard' | 'map' | 'clipboard' | 'package' | 'chart' | 'settings' 
  | 'warehouse' | 'chevron' | 'help' | 'bell' | 'user' | 'sparkles' 
  | 'trend' | 'download' | 'sliders' | 'search'

type Row = {
  time: string
  user: string
  initials: string
  action: 'Check-Out' | 'Restock'
  location: string
  item: string
  detail: string
}

const Icon = ({ name, className = 'size-4', strokeWidth = 1.8 }: { name: IconName; className?: string; strokeWidth?: number }) => {
  const paths: Record<IconName, React.ReactNode> = {
    dashboard: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></>,
    map: <><path d="m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3Z" /><path d="M9 3v15M15 6v15" /></>,
    clipboard: <><rect x="5" y="4" width="14" height="17" rx="2" /><path d="M9 4.5V3h6v1.5M8 9h8M8 13h8M8 17h5" /></>,
    package: <><path d="m21 8-9-5-9 5 9 5 9-5Z" /><path d="M3 8v9l9 5 9-5V8M12 13v9" /></>,
    chart: <><path d="M4 19V5M4 19h17" /><path d="m7 15 4-5 3 3 5-7" /></>,
    settings: <><path d="M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z" /><path d="m19.4 15 .1.1a2 2 0 0 1-2.8 2.8l-.1-.1a2 2 0 0 0-3.4 1.4v.2a2 2 0 0 1-4 0v-.2a2 2 0 0 0-3.4-1.4l-.1.1a2 2 0 0 1-2.8-2.8l-.1-.1A2 2 0 0 0 1.7 12a2 2 0 0 0 1.3-3.4l-.1-.1a2 2 0 0 1 2.8-2.8l.1.1A2 2 0 0 0 9.2 4.4v-.2a2 2 0 0 1 4 0v.2a2 2 0 0 0 3.4 1.4l-.1-.1a2 2 0 0 1 2.8 2.8l-.1.1A2 2 0 0 0 20.8 12a2 2 0 0 0-1.4 3Z" /></>,
    warehouse: <><path d="m3 10 9-6 9 6v10H3Z" /><path d="M7 20v-6h10v6M7 10h.01M12 10h.01M17 10h.01" /></>,
    chevron: <path d="m7 10 5 5 5-5" />,
    help: <><circle cx="12" cy="12" r="9" /><path d="M9.5 9a2.5 2.5 0 1 1 4.2 1.8c-1 .8-1.7 1.2-1.7 2.7M12 17h.01" /></>,
    bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" /></>,
    user: <><circle cx="12" cy="8" r="3.5" /><path d="M5 21a7 7 0 0 1 14 0" /></>,
    sparkles: <><path d="m12 3-1.2 4.8L6 9l4.8 1.2L12 15l1.2-4.8L18 9l-4.8-1.2Z" /><path d="m19 15-.6 2.4L16 18l2.4.6L19 21l.6-2.4L22 18l-2.4-.6Z" /></>,
    trend: <><path d="M3 17 9 11l4 4 8-9" /><path d="M15 6h6v6" /></>,
    download: <><path d="M12 3v12M7 10l5 5 5-5M4 21h16" /></>,
    sliders: <><path d="M4 6h16M4 12h16M4 18h16" /><circle cx="8" cy="6" r="2" /><circle cx="15" cy="12" r="2" /><circle cx="11" cy="18" r="2" /></>,
    search: <><circle cx="10.5" cy="10.5" r="6.5" /><path d="m16 16 5 5" /></>,
  }

  return (
    <svg aria-hidden="true" className={className} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round">
      {paths[name]}
    </svg>
  )
}

const ledgerRows: Row[] = [
  { time: 'Today, 10:42 AM', user: 'Olivia Chen', initials: 'OC', action: 'Check-Out', location: 'North Warehouse', item: 'Acetaminophen 500mg', detail: '24 units' },
  { time: 'Today, 9:15 AM', user: 'Marcus Johnson', initials: 'MJ', action: 'Restock', location: 'Main Facility', item: 'Nitrile Gloves · L', detail: '4 cases' },
  { time: 'Yesterday, 4:37 PM', user: 'Aisha Patel', initials: 'AP', action: 'Check-Out', location: 'Research Lab', item: 'PBS Buffer Solution', detail: '12 units' },
  { time: 'Yesterday, 1:08 PM', user: 'Daniel Kim', initials: 'DK', action: 'Restock', location: 'North Warehouse', item: 'Surgical Masks', detail: '8 cases' },
]

const chartValues = [38, 50, 42, 69, 57, 83, 62, 78, 68, 92, 74, 88]

function StatCard({ icon, eyebrow, value, label, accent }: { icon: IconName; eyebrow: string; value: string; label: string; accent: string }) {
  return (
    <article className="group rounded-xl border border-slate-200 bg-white p-5 shadow-[0_1px_2px_rgba(15,23,42,0.03)] transition hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md">
      <div className="flex items-start justify-between">
        <div className={`flex size-9 items-center justify-center rounded-lg ${accent}`}>
          <Icon name={icon} className="size-[18px]" />
        </div>
        <span className="rounded-full bg-emerald-50 px-2 py-1 text-[11px] font-semibold text-emerald-700">+12.8%</span>
      </div>
      <p className="mt-5 text-[12px] font-semibold uppercase tracking-[0.08em] text-slate-500">{eyebrow}</p>
      <p className="mt-1 text-[27px] font-semibold tracking-[-0.04em] text-slate-950">{value}</p>
      <p className="mt-1 text-[13px] text-slate-500">{label}</p>
    </article>
  )
}

export default function ReportsPage() {
  const [range, setRange] = useState('Last 30 Days')
  const [location, setLocation] = useState('All Locations')
  const [query, setQuery] = useState('')

  const filteredRows = useMemo(() => 
    ledgerRows.filter((row) => 
      `${row.user} ${row.action} ${row.location} ${row.item}`.toLowerCase().includes(query.toLowerCase())
    ), 
    [query]
  )

  return (
    <div className="mx-auto max-w-[1400px]">
      <div className="mb-7 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <h1 className="text-[20px] font-semibold tracking-[-0.025em] text-slate-950">Reports &amp; Analytics</h1>
          <p className="mt-1 text-[13px] text-slate-500">A clear view of your inventory performance and operational health.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <label className="relative">
            <span className="sr-only">Date range</span>
            <select 
              value={range} 
              onChange={(e) => setRange(e.target.value)} 
              className="h-9 appearance-none rounded-lg border border-slate-200 bg-white py-0 pl-3 pr-9 text-[12px] font-medium text-slate-700 shadow-sm outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100"
            >
              <option>Last 30 Days</option>
              <option>Last 90 Days</option>
              <option>This Year</option>
            </select>
            <Icon name="chevron" className="pointer-events-none absolute right-2.5 top-3 size-3.5 text-slate-400" />
          </label>
          <label className="relative">
            <span className="sr-only">Location scope</span>
            <select 
              value={location} 
              onChange={(e) => setLocation(e.target.value)} 
              className="h-9 appearance-none rounded-lg border border-slate-200 bg-white py-0 pl-3 pr-9 text-[12px] font-medium text-slate-700 shadow-sm outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100"
            >
              <option>All Locations</option>
              <option>Main Facility</option>
              <option>North Warehouse</option>
              <option>Research Lab</option>
            </select>
            <Icon name="chevron" className="pointer-events-none absolute right-2.5 top-3 size-3.5 text-slate-400" />
          </label>
          <button className="flex h-9 items-center gap-2 rounded-lg bg-slate-950 px-3.5 text-[12px] font-semibold text-white shadow-sm transition-colors hover:bg-blue-700">
            <Icon name="download" className="size-3.5" />Export
          </button>
        </div>
      </div> 

      <section className="grid gap-4 md:grid-cols-3">
        <StatCard icon="sparkles" eyebrow="Estimated Money Saved" value="$12,450" label="Year to date" accent="bg-amber-50 text-amber-600" />
        <StatCard icon="trend" eyebrow="Waste Reduction Rate" value="94.2%" label="Safe consumption rate" accent="bg-emerald-50 text-emerald-600" />
        <StatCard icon="clipboard" eyebrow="Active Audit Compliance" value="1,482" label="Immutable events logged" accent="bg-blue-50 text-blue-600" />
      </section> 

      <section className="mt-6 grid gap-4 xl:grid-cols-[1.45fr_1fr]">
        <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-[0_1px_2px_rgba(15,23,42,0.03)]">
          <div className="flex items-start justify-between">
            <div>
              <h2 className="text-[14px] font-semibold text-slate-900">Monthly High-Turnover Consumption</h2>
              <p className="mt-1 text-[12px] text-slate-500">Units consumed across all locations</p>
            </div>
            <button aria-label="Chart filters" className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100">
              <Icon name="sliders" />
            </button>
          </div>
          <div className="mt-6 flex h-[220px] gap-3">
            <div className="flex flex-col justify-between pb-6 text-[10px] text-slate-400">
              <span>100k</span><span>75k</span><span>50k</span><span>25k</span><span>0</span>
            </div>
            <div className="relative flex-1">
              <div className="absolute inset-0 flex flex-col justify-between pb-6">
                <span className="border-t border-dashed border-slate-200" />
                <span className="border-t border-dashed border-slate-200" />
                <span className="border-t border-dashed border-slate-200" />
                <span className="border-t border-dashed border-slate-200" />
                <span className="border-t border-slate-300" />
              </div>
              <div className="absolute inset-x-0 top-0 flex h-[180px] items-end gap-2 sm:gap-3">
                {chartValues.map((value, index) => (
                  <div key={index} className="group relative flex h-full flex-1 items-end">
                    <div className="w-full rounded-t bg-blue-500/85 transition group-hover:bg-blue-600" style={{ height: `${value}%` }} />
                    <span className="absolute -top-5 left-1/2 hidden -translate-x-1/2 text-[10px] font-semibold text-slate-600 group-hover:block">{value}k</span>
                  </div>
                ))}
              </div>
              <div className="absolute inset-x-0 bottom-0 flex justify-between text-[10px] text-slate-400">
                <span>Jan</span><span>Mar</span><span>May</span><span>Jul</span><span>Sep</span><span>Nov</span>
              </div>
            </div>
          </div>
        </article> 

        <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-[0_1px_2px_rgba(15,23,42,0.03)]">
          <div className="flex items-start justify-between">
            <div>
              <h2 className="text-[14px] font-semibold text-slate-900">Inventory Health &amp; Expiration</h2>
              <p className="mt-1 text-[12px] text-slate-500">Current stock status breakdown</p>
            </div>
            <span className="flex size-8 items-center justify-center rounded-lg bg-slate-50 text-slate-500">
              <Icon name="package" />
            </span>
          </div>
          <div className="mt-7 space-y-6">
            {[
              { label: 'Safe', value: '72%', count: '8,920 items', color: 'bg-emerald-500' }, 
              { label: 'Expiring Soon', value: '21%', count: '2,602 items', color: 'bg-amber-400' }, 
              { label: 'Expired', value: '7%', count: '864 items', color: 'bg-rose-400' }
            ].map((item) => (
              <div key={item.label}>
                <div className="mb-2 flex items-center justify-between">
                  <span className="flex items-center gap-2 text-[12px] font-medium text-slate-700">
                    <span className={`size-2 rounded-full ${item.color}`} />
                    {item.label}
                  </span>
                  <span className="text-[12px] font-semibold text-slate-800">{item.value}</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                  <div className={`h-full rounded-full ${item.color}`} style={{ width: item.value }} />
                </div>
                <p className="mt-1.5 text-[11px] text-slate-400">{item.count}</p>
              </div>
            ))}
          </div>
        </article>
      </section> 

      <section className="mt-6 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-[0_1px_2px_rgba(15,23,42,0.03)]">
        <div className="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-[14px] font-semibold text-slate-900">Compliance &amp; Activity Ledger</h2>
            <p className="mt-1 text-[12px] text-slate-500">Immutable record of inventory activity</p>
          </div>
          <div className="relative">
            <Icon name="search" className="absolute left-3 top-2.5 size-3.5 text-slate-400" />
            <input 
              value={query} 
              onChange={(e) => setQuery(e.target.value)} 
              placeholder="Search ledger..." 
              className="h-8 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-[12px] outline-none placeholder:text-slate-400 focus:border-blue-400 focus:bg-white focus:ring-2 focus:ring-blue-100 sm:w-[220px]" 
            />
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left">
            <thead className="bg-slate-50/80">
              <tr className="border-b border-slate-100 text-[10px] font-bold uppercase tracking-[0.08em] text-slate-400">
                <th className="px-5 py-3">Timestamp</th>
                <th className="px-5 py-3">User</th>
                <th className="px-5 py-3">Action Type</th>
                <th className="px-5 py-3">Location</th>
                <th className="px-5 py-3">Item Details</th>
                <th className="px-5 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredRows.map((row) => (
                <tr key={`${row.time}-${row.user}`} className="group hover:bg-slate-50/70">
                  <td className="whitespace-nowrap px-5 py-4 text-[12px] text-slate-500">{row.time}</td>
                  <td className="px-5 py-4">
                    <div className="flex items-center gap-2.5">
                      <span className="flex size-7 items-center justify-center rounded-full bg-slate-100 text-[10px] font-bold text-slate-600">{row.initials}</span>
                      <span className="whitespace-nowrap text-[12px] font-medium text-slate-800">{row.user}</span>
                    </div>
                  </td>
                  <td className="px-5 py-4">
                    <span className={`inline-flex rounded-full px-2 py-1 text-[10px] font-semibold ${row.action === 'Restock' ? 'bg-emerald-50 text-emerald-700' : 'bg-blue-50 text-blue-700'}`}>
                      {row.action}
                    </span>
                  </td>
                  <td className="px-5 py-4 text-[12px] text-slate-600">{row.location}</td>
                  <td className="px-5 py-4">
                    <p className="text-[12px] font-medium text-slate-800">{row.item}</p>
                    <p className="mt-0.5 text-[11px] text-slate-400">{row.detail}</p>
                  </td>
                  <td className="px-5 py-4 text-right">
                    <button aria-label={`View ${row.item}`} className="text-[11px] font-semibold text-blue-600 opacity-0 transition group-hover:opacity-100">
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {filteredRows.length === 0 && (
            <p className="px-5 py-8 text-center text-sm text-slate-500">No ledger entries found.</p>
          )}
        </div>
      </section>
    </div>
  )
}