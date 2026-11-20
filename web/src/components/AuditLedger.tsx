import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CatalogService } from '../api/CatalogService'
import { useCurrentUser } from '../hooks/useCurrentUser'

export default function AuditLedger() {
  const clinicId = useCurrentUser()?.organizationId
  const [search, setSearch] = useState('')
  const { data = [], isLoading, isError, refetch } = useQuery({
    queryKey: ['compliance-audit-logs', clinicId],
    queryFn: () => new CatalogService().getAuditLogs(),
    retry: 1,
  })
  const needle = search.trim().toLowerCase()
  const rows = data.filter(entry =>
    [entry.userEmail, entry.actionType, entry.itemDetails, entry.locationContext]
      .some(value => value.toLowerCase().includes(needle))
  )

  return (
    <section aria-labelledby="clinic-audit-heading" className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-5 py-4">
        <div>
          <h2 id="clinic-audit-heading" className="text-sm font-semibold">Clinic audit history</h2>
          <p className="mt-1 text-xs text-slate-500">The 50 most recent events across all clinic locations.</p>
        </div>
        <input type="search" aria-label="Search audit history" placeholder="Search audit history"
          value={search} onChange={event => setSearch(event.target.value)}
          className="h-9 rounded-lg border border-slate-200 bg-slate-50 px-3 text-xs" />
      </div>
      {isError ? (
        <div role="alert" className="p-5 text-sm">
          Audit history could not be loaded. <button onClick={() => refetch()} className="font-medium text-blue-700">Try again</button>
        </div>
      ) : isLoading ? (
        <p role="status" className="p-5 text-sm text-slate-500">Loading audit history…</p>
      ) : rows.length === 0 ? (
        <p className="p-5 text-sm text-slate-500">{needle ? 'No matching events.' : 'No audit events have been recorded.'}</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-xs text-slate-700">
            <thead className="bg-slate-50 text-slate-500">
              <tr>{['Timestamp', 'User', 'Action', 'Details', 'Location'].map(label => (
                <th key={label} scope="col" className="px-5 py-3">{label}</th>
              ))}</tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {rows.map(entry => (
                <tr key={entry.id}>
                  <td className="whitespace-nowrap px-5 py-4"><time dateTime={entry.timestamp}>{new Date(entry.timestamp).toLocaleString()}</time></td>
                  <td className="px-5 py-4">{entry.userEmail}</td>
                  <td className="px-5 py-4 font-medium">{entry.actionType}</td>
                  <td className="px-5 py-4">{entry.itemDetails}</td>
                  <td className="px-5 py-4">{entry.locationContext}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
