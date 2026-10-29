
import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  isLoading: boolean;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [search, setSearch] = useState(filters.search);
  const [state, setState] = useState<State>({ isLoading: true });
  const requestId = useRef(0);

  // Only delay search input, not other filters.
  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(filters.search), DEBOUNCE_MS);
    return () => window.clearTimeout(timer);
  }, [filters.search]);

  const {
    status, priority, categoryId, customerId,
    assignedAgentId, unassignedOnly, page, pageSize,
    sortBy, sortDirection,
  } = filters;

  const load = async (currentFilters: TicketFilters) => {
    const id = ++requestId.current;
    setState((current) => ({ ...current, isLoading: true, error: undefined }));

    try {
      const data = await ticketsApi.getTickets(currentFilters);

      if (id === requestId.current) {
        setState({ isLoading: false, data, error: undefined });
      }
    } catch (error) {
      if (id === requestId.current) {
        const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
        setState((current) => ({ ...current, isLoading: false, error: message }));
      }
    }
  };

  useEffect(() => {
    void load({
      search, status, priority, categoryId, customerId,
      assignedAgentId, unassignedOnly, page, pageSize,
      sortBy, sortDirection,
    });

    // Invalidate requests if filters change or the component unmounts.
    return () => { requestId.current++; };
  }, [
    search, status, priority, categoryId, customerId,
    assignedAgentId, unassignedOnly, page, pageSize,
    sortBy, sortDirection,
  ]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => setFilters(emptyFilters);

  const reload = () => load({ ...filters, search });

  return { filters, updateFilters, resetFilters, reload, ...state };
}
