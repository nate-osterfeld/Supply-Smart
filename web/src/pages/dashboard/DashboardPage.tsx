'use client'

import type { ReactNode } from 'react'
import {
  AlertCircle,
  AlertTriangle,
  ArrowUpRight,
  ChevronRight,
  Clock,
  Package,
  Plus,
  Zap,
} from 'lucide-react'
import { useDashboard } from '../../hooks/useDashboard'

type ChangeType = 'positive' | 'warning' | 'critical' | 'neutral'
type ActionType = 'checkout' | 'restock' | 'adjustment' | 'system'

export default function DashboardPage({ onNavigate }: { onNavigate?: (screen: string) => void }) {
  const { metrics, forecast, auditLogs, isLoadingMetrics, isLoadingForecast } = useDashboard();

  return (
    <div className="p-8">
      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Dashboard Overview</h1>
          <p className="text-sm text-slate-500">Real-time inventory health and procedural demand warnings.</p>
        </div>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <KPICard 
            title="Total Inventory Value" 
            value={isLoadingMetrics ? "Loading..." : `$${(metrics?.totalInventoryValue ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`} 
            change="+8.2%" 
            changeType="positive" 
            icon={<Package className="size-5 text-blue-600" />} 
          />
          <KPICard 
            title="Expiring Soon" 
            value={isLoadingMetrics ? "Loading..." : `${metrics?.expiringSoonCount ?? 0} items`} 
            change="within 30 days" 
            changeType="warning" 
            icon={<Clock className="size-5 text-amber-600" />} 
          />
          <KPICard 
            title="Active Low-Stock Alerts" 
            value={isLoadingMetrics ? "Loading..." : `${metrics?.activeLowStockAlertsCount ?? 0} items`} 
            change="below minimum" 
            changeType="critical" 
            icon={<AlertCircle className="size-5 text-rose-600" />} 
          />
          <KPICard 
            title="Pending Restocks" 
            value={isLoadingMetrics ? "Loading..." : `${metrics?.pendingRestocksCount ?? 0} orders`} 
            change="auto-suggested" 
            changeType="neutral" 
            icon={<Zap className="size-5 text-slate-600" />} 
          />
        </div>

        <section className="rounded-lg border border-amber-200 bg-amber-50 p-6 dark:border-amber-900/30 dark:bg-amber-950/20">
          <div className="flex gap-4">
            <AlertTriangle className="size-5 shrink-0 text-amber-600" />
            <div className="flex-1">
              <h2 className="font-semibold text-amber-950 dark:text-amber-100">Upcoming Job / Procedure Demand Warning</h2>
              <p className="mt-1 text-sm text-amber-900 dark:text-amber-200">
                {isLoadingForecast ? "Loading forecast summary..." : forecast?.deficitMessage}
              </p>
              <button 
                onClick={() => onNavigate?.('forecast')}
                className="mt-3 inline-flex h-9 items-center gap-2 rounded-md bg-amber-600 px-4 text-sm font-medium text-white hover:bg-amber-700"
              >
                View Schedule &amp; Forecast <ChevronRight className="size-4" />
              </button>
            </div>
          </div>
        </section>

        <div className="grid gap-6 lg:grid-cols-2">
          <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <div className="border-b border-slate-100 p-6 dark:border-slate-800">
              <h2 className="font-semibold text-slate-900 dark:text-white">Expiration Risk Breakdown</h2>
              <p className="mt-1 text-sm text-slate-500">Health status of your inventory</p>
            </div>
            <div className="flex flex-col gap-6 p-6">
              <HealthBar label="Safe Stock" percentage={isLoadingMetrics ? 0 : (metrics?.safeStockPercentage ?? 100)} color="emerald" />
              <HealthBar label="Expiring Soon" percentage={isLoadingMetrics ? 0 : (metrics?.expiringSoonPercentage ?? 0)} color="amber" />
              <HealthBar label="Expired" percentage={isLoadingMetrics ? 0 : (metrics?.expiredPercentage ?? 0)} color="rose" />
            </div>
          </section>

          <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <div className="border-b border-slate-100 p-6 dark:border-slate-800">
              <h2 className="font-semibold text-slate-900 dark:text-white">Consumption Trends</h2>
              <p className="mt-1 text-sm text-slate-500">Weekly usage patterns for high-volume supplies</p>
            </div>
            <div className="p-6">
              <svg className="h-48 w-full" viewBox="0 0 400 160" role="img" aria-label="Weekly consumption chart showing usage spikes">
                <defs>
                  <linearGradient id="chartGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                    <stop offset="0%" stopColor="rgb(59,130,246)" stopOpacity=".3" />
                    <stop offset="100%" stopColor="rgb(59,130,246)" stopOpacity="0" />
                  </linearGradient>
                </defs>
                <polyline points="20,120 60,80 100,100 140,40 180,60 220,30 260,70 300,50 340,90 380,35" fill="none" stroke="rgb(59,130,246)" strokeWidth="2" />
                <polygon points="20,120 60,80 100,100 140,40 180,60 220,30 260,70 300,50 340,90 380,35 380,160 20,160" fill="url(#chartGradient)" />
                <line x1="20" y1="160" x2="380" y2="160" stroke="rgb(203,213,225)" />
              </svg>
            </div>
          </section>
        </div>

        <div className="flex flex-wrap gap-3">
          <button className="inline-flex h-9 items-center gap-2 rounded-md bg-blue-600 px-4 text-sm font-medium text-white hover:bg-blue-700">
            <Plus className="size-4" />Scan / Add Item
          </button>
          <button className="h-9 rounded-md border border-slate-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
            Check Out Items
          </button>
          <button className="h-9 rounded-md border border-slate-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
            Generate Purchase Order
          </button>
        </div>

        <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
          <div className="border-b border-slate-100 p-6 dark:border-slate-800">
            <h2 className="font-semibold text-slate-900 dark:text-white">Recent Activity</h2>
            <p className="mt-1 text-sm text-slate-500">Audit trail of recent inventory changes</p>
          </div>
          <div className="overflow-x-auto p-6">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 dark:border-slate-700">
                  <th className="px-4 py-3 font-medium text-slate-600">Timestamp</th>
                  <th className="px-4 py-3 font-medium text-slate-600">User</th>
                  <th className="px-4 py-3 font-medium text-slate-600">Action</th>
                  <th className="px-4 py-3 font-medium text-slate-600">Item Details</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                {auditLogs.map((log) => (
                  <AuditRow 
                    key={log.id} 
                    timestamp={new Date(log.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} 
                    user={log.userEmail} 
                    action={log.actionType} 
                    actionType={log.actionType.toLowerCase() as ActionType} 
                    item={log.itemDetails} 
                  />
                ))}
              </tbody>
            </table>
            <button 
              onClick={() => onNavigate?.('catalog')}
              className="mt-4 flex w-full items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium text-blue-600 hover:bg-blue-50"
            >
              View Full Audit Log <ChevronRight className="size-4" />
            </button>
          </div>
        </section>
      </div>
    </div>
  )
}

function KPICard({ title, value, change, changeType, icon }: { title: string; value: string; change: string; changeType: ChangeType; icon: ReactNode }) {
  const colors: Record<ChangeType, string> = { 
    positive: 'bg-emerald-50 text-emerald-700', 
    warning: 'bg-amber-50 text-amber-700', 
    critical: 'bg-rose-50 text-rose-700', 
    neutral: 'bg-slate-100 text-slate-700' 
  }
  
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-6 dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm font-medium text-slate-600 dark:text-slate-400">{title}</p>
          <p className="mt-2 text-2xl font-bold text-slate-900 dark:text-white">{value}</p>
          <span className={`mt-2 inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium ${colors[changeType]}`}>
            {changeType === 'positive' && <ArrowUpRight className="size-3" />}
            {change}
          </span>
        </div>
        <div className="rounded-lg bg-slate-100 p-2.5 dark:bg-slate-800">{icon}</div>
      </div>
    </section>
  )
}

function HealthBar({ label, percentage, color }: { label: string; percentage: number; color: 'emerald' | 'amber' | 'rose' }) {
  const colors = { emerald: 'bg-emerald-500', amber: 'bg-amber-500', rose: 'bg-rose-500' }
  return (
    <div>
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-slate-700 dark:text-slate-300">{label}</span>
        <span className="text-sm font-semibold text-slate-900 dark:text-white">{percentage}%</span>
      </div>
      <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-700">
        <div className={`h-full ${colors[color]}`} style={{ width: `${percentage}%` }} />
      </div>
    </div>
  )
}

function AuditRow({ timestamp, user, action, actionType, item }: { timestamp: string; user: string; action: string; actionType: ActionType; item: string }) {
  const colors: Record<string, string> = { 
    checkout: 'bg-blue-100 text-blue-800', 
    restock: 'bg-emerald-100 text-emerald-800', 
    adjustment: 'bg-amber-100 text-amber-800',
    system: 'bg-slate-100 text-slate-800'
  }
  
  return (
    <tr>
      <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{timestamp}</td>
      <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{user}</td>
      <td className="px-4 py-3">
        <span className={`inline-flex rounded px-2 py-1 text-xs font-medium ${colors[actionType] || colors.system}`}>{action}</span>
      </td>
      <td className="px-4 py-3 text-slate-700 dark:text-slate-300">{item}</td>
    </tr>
  )
}