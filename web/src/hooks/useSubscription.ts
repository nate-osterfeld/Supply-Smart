import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { SubscriptionService } from '../api/SubscriptionService';
import type { UpdateSubscriptionRequestDto } from '../models';

export function useSubscription() {
    const queryClient = useQueryClient();
    const service = new SubscriptionService();

    const subscriptionQuery = useQuery({
        queryKey: ['subscription-details'],
        queryFn: () => service.getSubscriptionDetails(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const updatePlanMutation = useMutation({
        mutationFn: (dto: UpdateSubscriptionRequestDto) => service.updateSubscriptionPlan(dto),
        onSuccess: (data) => {
            if (!data.updatedImmediately && data.checkoutUrl) {
                window.location.href = data.checkoutUrl;
                return;
            }
            queryClient.invalidateQueries({ queryKey: ['subscription-details'] });
        },
    });

    const openPortalMutation = useMutation({
        mutationFn: () => service.createPortalSession(),
        onSuccess: (data) => {
            if (data.portalUrl) {
                window.location.href = data.portalUrl;
            }
        },
    });

    const verifySessionMutation = useMutation({
        mutationFn: (sessionId: string) => service.verifySession({ sessionId }),
        onSuccess: (updatedDetails) => {
            queryClient.setQueryData(['subscription-details'], updatedDetails);
            queryClient.invalidateQueries({ queryKey: ['subscription-details'] });
        },
    });

    return {
        subscription: subscriptionQuery.data ?? null,
        isLoadingSubscription: subscriptionQuery.isLoading,
        isErrorSubscription: subscriptionQuery.isError,
        updatePlan: updatePlanMutation.mutateAsync,
        isUpdatingPlan: updatePlanMutation.isPending,
        openCustomerPortal: openPortalMutation.mutateAsync,
        isOpeningPortal: openPortalMutation.isPending,
        verifySession: verifySessionMutation.mutateAsync,
        isVerifyingSession: verifySessionMutation.isPending,
    };
}