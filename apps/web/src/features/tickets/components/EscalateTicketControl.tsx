
import { useState } from 'react';
import type { FormEvent } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { TicketDetail, TicketPriority } from '../../../types/api';

const priorities: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];

interface EscalateTicketControlProps {
  ticket: TicketDetail;
  onUpdated: (ticket: TicketDetail) => void;
}

export function EscalateTicketControl({
  ticket,
  onUpdated,
}: EscalateTicketControlProps) {
  const [newPriority, setNewPriority] = useState<TicketPriority>('Critical');
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string>();

  const availablePriorities = priorities.filter(
    (priority) => priorities.indexOf(priority) > priorities.indexOf(ticket.priority)
  );

  if (availablePriorities.length === 0) {
    return null;
  }

  const selectedPriority = availablePriorities.includes(newPriority)
    ? newPriority
    : availablePriorities[0];

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!reason.trim()) {
      setError('Please provide an escalation reason.');
      return;
    }

    setSaving(true);
    setError(undefined);

    try {
      const updated = await ticketsApi.escalateTicket(
        ticket.id,
        selectedPriority,
        reason.trim()
      );
      onUpdated(updated);
      setReason('');
    } catch (caught) {
      setError(toErrorMessage(caught, 'Could not escalate the ticket.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="card">
      <h2>Escalate Ticket</h2>

      <form onSubmit={(event) => void submit(event)}>
        <div className="field">
          <label htmlFor="escalation-priority">New priority</label>
          <select
            id="escalation-priority"
            value={selectedPriority}
            onChange={(event) =>
              setNewPriority(event.target.value as TicketPriority)
            }
            disabled={saving}
          >
            {availablePriorities.map((priority) => (
              <option key={priority} value={priority}>
                {priority}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label htmlFor="escalation-reason">Reason</label>
          <textarea
            id="escalation-reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            required
            maxLength={1000}
            disabled={saving}
          />
        </div>

        <button
          type="submit"
          className="button"
          disabled={saving || !reason.trim()}
        >
          {saving ? 'Escalating...' : 'Escalate Ticket'}
        </button>

        {error && (
          <p className="field__error" role="alert">
            {error}
          </p>
        )}
      </form>
    </div>
  );
}
