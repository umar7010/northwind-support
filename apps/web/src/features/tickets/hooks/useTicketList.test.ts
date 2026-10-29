import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { page, ticketListItem } from '../../../test/fixtures';
import { useTicketList } from './useTicketList';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    getTickets: vi.fn(),
  },
}));

const getTickets = vi.mocked(ticketsApi.getTickets);

describe('useTicketList', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    getTickets.mockResolvedValue(page([ticketListItem()]));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('searches with the text the user typed, once the typing settles', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(1);

    act(() => {
      result.current.updateFilters({ search: 'invoice' });
    });

    // Still inside the debounce window: no second request yet.
    expect(getTickets).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(2);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'invoice', page: 1 }),
    );

    expect(result.current.isLoading).toBe(false);
    expect(result.current.data?.items).toHaveLength(1);
  });

  it('fetches tickets when status changes without waiting for search debounce', async () => {
  const { result } = renderHook(() => useTicketList());

  // Allow the initial request to complete.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });

  expect(getTickets).toHaveBeenCalledTimes(1);

  // Change only the status filter.
  act(() => {
    result.current.updateFilters({ status: 'InProgress' });
  });

  // Status changes should trigger a request immediately.
  await act(async () => {
    await Promise.resolve();
  });

  expect(getTickets).toHaveBeenCalledTimes(2);

  expect(getTickets).toHaveBeenLastCalledWith(
    expect.objectContaining({
      status: 'InProgress',
      page: 1,
    }),
  );
});

it('fetches immediately when status changes during search debounce', async () => {
  const { result } = renderHook(() => useTicketList());

  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });

  expect(getTickets).toHaveBeenCalledTimes(1);

  // User starts typing.
  act(() => {
    result.current.updateFilters({ search: 'outage' });
  });

  // Only 100 ms have passed, so search is still debouncing.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(100);
  });

  expect(getTickets).toHaveBeenCalledTimes(1);

  // User changes status before the search debounce finishes.
  act(() => {
    result.current.updateFilters({ status: 'InProgress' });
  });

  await act(async () => {
    await Promise.resolve();
  });

  // The status change must not wait for search debounce.
  expect(getTickets).toHaveBeenCalledTimes(2);

  expect(getTickets).toHaveBeenLastCalledWith(
    expect.objectContaining({
      status: 'InProgress',
      page: 1,
    }),
  );
});

});
