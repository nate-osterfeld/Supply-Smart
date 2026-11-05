import { useEffect, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { EmailTemplateService, type EmailTemplate } from '../../api/EmailTemplateService'

const service = new EmailTemplateService()
const button = 'rounded-lg border border-slate-300 px-4 py-2 text-xs font-semibold disabled:cursor-not-allowed disabled:opacity-50'
const field = 'mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm'
type Props = {
  addToast: (title: string, message: string, type?: 'success' | 'error') => void
  onDirtyChange: (dirty: boolean) => void
}

export default function EmailTemplatesPanel({ addToast, onDirtyChange }: Props) {
  const organizationId = localStorage.getItem('supplysmart_org_id')
  const queryClient = useQueryClient()
  const queryKey = ['email-templates', organizationId]
  const query = useQuery({
    queryKey,
    queryFn: () => service.getTemplates(),
    enabled: localStorage.getItem('supplysmart_role') === 'Admin',
    refetchOnWindowFocus: false,
    retry: 1,
  })
  const [trigger, setTrigger] = useState('low-stock')
  if (localStorage.getItem('supplysmart_role') !== 'Admin') return <p className="rounded-xl border border-slate-200 bg-white p-6 text-sm">Only administrators can manage email templates.</p>
  if (query.isPending) return <p role="status" className="p-6 text-sm">Loading email templates...</p>
  if (query.error) return <div role="alert" className="rounded-xl border border-red-200 bg-white p-6 text-sm">
    <p>{query.error.message}</p><button className={button + ' mt-3'} onClick={() => void query.refetch()}>Try again</button>
  </div>
  const selected = query.data.find(t => t.trigger === trigger)
  if (!selected) return <p role="alert">This template is unavailable. Reload the page and try again.</p>
  return <TemplateEditor key={selected.trigger + ':' + selected.revision} template={selected} templates={query.data}
    addToast={addToast} onDirtyChange={onDirtyChange} onSelect={setTrigger}
    onReload={async () => {
      const result = await query.refetch()
      if (result.error) throw result.error
      return result.data!.find(t => t.trigger === trigger)!
    }}
    onSaved={saved => {
      queryClient.setQueryData<EmailTemplate[]>(queryKey, existing => existing?.map(t => t.trigger === saved.trigger ? saved : t))
      queryClient.invalidateQueries({ queryKey: ['compliance-audit-logs'] })
    }} />
}

function TemplateEditor({ template, templates, onSelect, onReload, onSaved, addToast, onDirtyChange }: Props & {
  template: EmailTemplate; templates: EmailTemplate[]; onSelect: (trigger: string) => void
  onReload: () => Promise<EmailTemplate>; onSaved: (template: EmailTemplate) => void
}) {
  const [subject, setSubject] = useState(template.subject)
  const [body, setBody] = useState(template.body)
  const [pending, setPending] = useState<'save' | 'test' | 'reload' | null>(null)
  const [error, setError] = useState('')
  const dirty = subject !== template.subject || body !== template.body
  useEffect(() => {
    if (!dirty) return
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = '' }
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [dirty])
  const sample: Record<string, string> = {
    organization_name: 'Your organization', item_name: 'Sample gloves', supply_room: 'Sample supply room',
    current_quantity: template.trigger === 'out-of-stock' ? '0' : '4', minimum_threshold: '10',
    week_ending: new Date().toISOString().slice(0, 10),
    inventory_summary: 'Sample gloves: 4 remaining (minimum 10).\nSample masks: 0 remaining (minimum 20).',
  }
  const preview = (text: string) => text.replace(/\{\{\s*([a-z_]+)\s*\}\}/g, (match, name: string) => sample[name] ?? match)
  const mayDiscard = () => !dirty || window.confirm('Discard the unsaved changes to this email template?')
  const run = async (operation: 'save' | 'test') => {
    if (pending) return
    setPending(operation); setError('')
    try {
      const draft = { subject, body, revision: template.revision }
      if (operation === 'save') {
        const saved = await service.saveTemplate(template.trigger, draft)
        setSubject(saved.subject); setBody(saved.body)
        onDirtyChange(false)
        onSaved(saved)
        addToast('Template saved', 'Your organization will keep this message when you reload the page.')
      } else {
        const sent = await service.sendTest(template.trigger, draft)
        addToast('Test email sent', 'Sent to ' + sent.recipient + ' using sample inventory.')
      }
    } catch (err) { setError(err instanceof Error ? err.message : 'The request could not be completed.') }
    finally { setPending(null) }
  }
  return <div className="space-y-5">
    <div><h2 className="text-lg font-bold">Low-Stock Email Templates</h2>
      <p className="mt-1 text-sm text-slate-500">Write the emails your organization will use for inventory updates.</p></div>
    <div className="rounded-xl border border-slate-200 bg-white p-5 sm:p-6">
      <form onSubmit={event => { event.preventDefault(); void run('save') }}>
        <fieldset disabled={pending !== null} className="space-y-5">
          <label className="block text-sm font-semibold">Email trigger
            <select className={field} value={template.trigger} onChange={event => {
              if (mayDiscard()) { onDirtyChange(false); onSelect(event.target.value) }
            }}>{templates.map(t => <option key={t.trigger} value={t.trigger}>{t.label}</option>)}</select>
          </label>
          <label className="block text-sm font-semibold">Subject
            <input required maxLength={200} className={field} value={subject} onChange={e => { setSubject(e.target.value); onDirtyChange(e.target.value !== template.subject || body !== template.body) }} />
          </label>
          <label className="block text-sm font-semibold">Email message
            <textarea required maxLength={8000} rows={9} className={field + ' resize-y leading-6'} value={body} onChange={e => { setBody(e.target.value); onDirtyChange(subject !== template.subject || e.target.value !== template.body) }} />
          </label>
          <div className="text-xs leading-6 text-slate-500"><p>Use plain text. These placeholders will be replaced with inventory details:</p>
            <div className="mt-1 flex flex-wrap gap-2">{template.supportedVariables.map(v => <code key={v} className="rounded bg-slate-100 px-2">{'{{' + v + '}}'}</code>)}</div>
          </div>
          <div className="rounded-lg bg-slate-50 p-4">
            <h3 className="text-xs font-bold uppercase tracking-wide text-slate-500">Preview with sample inventory</h3>
            <p className="mt-3 break-words text-sm font-semibold">{preview(subject)}</p>
            <p className="mt-3 whitespace-pre-wrap break-words text-sm leading-6 text-slate-600">{preview(body)}</p>
          </div>
        </fieldset>
        <div className="mt-5 text-xs text-slate-500">
          {template.updatedAtUtc ? <p>Last saved by {template.updatedBy} on {new Date(template.updatedAtUtc.endsWith('Z') ? template.updatedAtUtc : template.updatedAtUtc + 'Z').toLocaleString()}.</p> : <p>Default template. No changes saved yet.</p>}
          {dirty && <p className="mt-1 font-semibold text-amber-700">You have unsaved changes.</p>}
        </div>
        {error && <p role="alert" className="mt-4 text-sm text-red-700">{error}</p>}
        <div className="mt-5 flex flex-wrap gap-2">
          <button type="submit" className={button + ' bg-slate-900 text-white'} disabled={!!pending || (!dirty && template.revision > 0)}>{pending === 'save' ? 'Saving...' : 'Save template'}</button>
          <button type="button" className={button} disabled={!!pending || !subject.trim() || !body.trim()} onClick={() => void run('test')}>{pending === 'test' ? 'Sending...' : 'Send test to my email'}</button>
          <button type="button" className={button} disabled={!!pending} onClick={async () => {
            if (!mayDiscard()) return
            setPending('reload'); setError('')
            try { const saved = await onReload(); setSubject(saved.subject); setBody(saved.body); onDirtyChange(false) }
            catch (err) { setError(err instanceof Error ? err.message : 'The template could not be reloaded.') }
            finally { setPending(null) }
          }}>Reload saved template</button>
        </div>
        <p className="mt-3 text-xs leading-5 text-slate-500">Test emails use the message currently shown, including unsaved edits, and go only to your account email. A test does not save your changes.</p>
      </form>
    </div>
    <p className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">Automatic inventory emails are not enabled yet. You can save templates and send test emails now.</p>
  </div>
}
