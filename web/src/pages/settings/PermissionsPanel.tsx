import { useEffect, useRef, useState } from 'react'
import { useUsers } from '../../hooks/useUsers'
import type { UserDto } from '../../models'

type Notice = (title: string, message: string, type?: 'success' | 'error') => void
type Action = { kind: 'invite' } | { kind: 'role' | 'revoke'; user: UserDto }
const field = 'mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm'
const button = 'rounded-lg border border-slate-300 px-3 py-2 text-xs font-semibold disabled:cursor-not-allowed disabled:opacity-50'
const primary = button + ' bg-slate-900 text-white hover:bg-slate-800'
const roles = ['Admin', 'Manager', 'Staff']

export default function PermissionsPanel({ addToast }: { addToast: Notice }) {
  const { users, isLoadingUsers, error, refreshUsers, inviteUser, deleteUser, updateRole } = useUsers()
  const [action, setAction] = useState<Action | null>(null)
  const currentEmail = localStorage.getItem('supplysmart_email')?.toLowerCase()
  if (localStorage.getItem('supplysmart_role') !== 'Admin') {
    return <p className="rounded-xl border border-slate-200 bg-white p-6 text-sm">Only administrators can manage user permissions.</p>
  }
  const submit = async (name: string, email: string, role: string) => {
    if (!action) return
    if (action.kind === 'invite') {
      await inviteUser({ fullName: name, email, role })
      addToast('Invitation sent', 'The new user has been emailed their sign-in details.')
    } else if (action.kind === 'role') {
      await updateRole({ userId: action.user.id, role })
      addToast('Role updated', 'The user will need to sign in again.')
    } else {
      await deleteUser(action.user.id)
      addToast('Access removed', 'This user can no longer access the organization.')
    }
    setAction(null)
  }
  return <div className="space-y-5">
    <div className="flex flex-wrap items-start justify-between gap-3">
      <div><h2 className="text-lg font-bold">User Permissions</h2>
        <p className="mt-1 text-sm text-slate-500">Invite team members, change roles, or remove access.</p></div>
      <button className={primary} onClick={() => setAction({ kind: 'invite' })}>Invite team member</button>
    </div>
    <p className="text-xs leading-5 text-slate-500">Admin access includes managing users and email templates. Another administrator must change your own role or remove your account.</p>
    {error ? <div role="alert" className="rounded-xl border border-red-200 bg-white p-5 text-sm">
      <p>{error.message}</p><button className={button + ' mt-3'} onClick={() => void refreshUsers()}>Try again</button>
    </div> : isLoadingUsers ? <p role="status" className="p-6 text-sm">Loading users...</p> :
    <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
      <table className="w-full text-left text-sm">
        <caption className="sr-only">Users in your organization</caption>
        <thead className="bg-slate-50 text-xs text-slate-500"><tr><th className="p-4">Team member</th><th className="p-4">Role</th><th className="p-4">Actions</th></tr></thead>
        <tbody>{users.map(user => {
          const ownAccount = user.email.toLowerCase() === currentEmail
          return <tr key={user.id} className="border-t border-slate-100">
            <td className="p-4"><p className="font-semibold">{user.fullName}{ownAccount ? ' (you)' : ''}</p><p className="mt-1 text-xs text-slate-500">{user.email}</p></td>
            <td className="p-4">{user.role}</td>
            <td className="p-4"><div className="flex flex-wrap gap-2">
              <button className={button} disabled={ownAccount} aria-label={'Change role for ' + user.fullName} onClick={() => setAction({ kind: 'role', user })}>Change role</button>
              <button className={button + ' text-red-700'} disabled={ownAccount} aria-label={'Remove access for ' + user.fullName} onClick={() => setAction({ kind: 'revoke', user })}>Remove access</button>
            </div></td>
          </tr>
        })}</tbody>
      </table>
      {users.length === 0 && <p className="p-5 text-sm text-slate-500">No users found.</p>}
    </div>}
    {action && <UserActionDialog action={action} onClose={() => setAction(null)} onSubmit={submit} />}
  </div>
}

function UserActionDialog({ action, onClose, onSubmit }: {
  action: Action; onClose: () => void; onSubmit: (name: string, email: string, role: string) => Promise<void>
}) {
  const dialog = useRef<HTMLDialogElement>(null)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState(action.kind === 'invite' ? 'Staff' : action.user.role)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  useEffect(() => { dialog.current?.showModal() }, [])
  const title = action.kind === 'invite' ? 'Invite team member' : action.kind === 'role' ? 'Change role' : 'Remove access'
  return <dialog ref={dialog} aria-labelledby="user-dialog-title" onCancel={event => { if (pending) event.preventDefault(); else onClose() }}
    className="fixed inset-0 m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl backdrop:bg-slate-950/40">
    <form onSubmit={async event => {
      event.preventDefault()
      if (pending) return
      setPending(true); setError('')
      try { await onSubmit(name.trim(), email.trim(), role) }
      catch (err) { setError(err instanceof Error ? err.message : 'The change could not be saved.'); setPending(false) }
    }}>
      <h3 id="user-dialog-title" className="font-bold">{title}</h3>
      <fieldset disabled={pending} className="mt-4 space-y-4">
        {action.kind === 'invite' ? <>
          <p className="text-sm text-slate-500">We will email this person their sign-in details.</p>
          <label className="block text-sm">Full name<input autoFocus required maxLength={150} className={field} value={name} onChange={e => setName(e.target.value)} /></label>
          <label className="block text-sm">Email address<input required type="email" maxLength={256} className={field} value={email} onChange={e => setEmail(e.target.value)} /></label>
        </> : <p className="text-sm text-slate-600">{action.user.fullName} ({action.user.email})</p>}
        {action.kind !== 'revoke' && <label className="block text-sm">Role<select className={field} value={role} onChange={e => setRole(e.target.value)}>{roles.map(r => <option key={r}>{r}</option>)}</select></label>}
        {action.kind === 'role' && <p className="text-sm text-slate-500">Saving a different role signs this user out. They can sign in again with their new role.</p>}
        {action.kind === 'revoke' && <p className="text-sm text-slate-500">This removes their account and ends their access. You would need to invite them again to restore it.</p>}
      </fieldset>
      {error && <p role="alert" className="mt-4 text-sm text-red-700">{error}</p>}
      <div className="mt-6 flex justify-end gap-2">
        <button type="button" className={button} disabled={pending} onClick={onClose}>Cancel</button>
        <button className={primary} disabled={pending || (action.kind === 'role' && role === action.user.role)}>{pending ? 'Please wait...' : title}</button>
      </div>
    </form>
  </dialog>
}
