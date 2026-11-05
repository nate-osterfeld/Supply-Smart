import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { UserService } from '../api/UserService';
import type { InviteUserDto } from '../models';

export function useUsers() {
    const queryClient = useQueryClient();
    const service = new UserService();
    const organizationId = localStorage.getItem('supplysmart_org_id');
    const usersQuery = useQuery({
        queryKey: ['tenant-users', organizationId],
        queryFn: () => service.getUsers(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token') && localStorage.getItem('supplysmart_role') === 'Admin',
    });
    const refresh = () => {
        queryClient.invalidateQueries({ queryKey: ['tenant-users', organizationId] });
        queryClient.invalidateQueries({ queryKey: ['compliance-audit-logs'] });
    };
    const inviteMutation = useMutation({ mutationFn: (dto: InviteUserDto) => service.inviteUser(dto), onSuccess: refresh });
    const deleteMutation = useMutation({ mutationFn: (userId: number) => service.deleteUser(userId), onSuccess: refresh });
    const roleMutation = useMutation({
        mutationFn: ({ userId, role }: { userId: number; role: string }) => service.updateRole(userId, role),
        onSuccess: refresh,
    });
    return {
        users: usersQuery.data ?? [],
        isLoadingUsers: usersQuery.isLoading,
        error: usersQuery.error,
        refreshUsers: usersQuery.refetch,
        inviteUser: inviteMutation.mutateAsync,
        deleteUser: deleteMutation.mutateAsync,
        updateRole: roleMutation.mutateAsync,
    };
}
