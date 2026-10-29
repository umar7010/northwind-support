
import { formatDateTime } from '../../../lib/format';
import type { TicketEscalation } from '../../../types/api';

interface EscalationHistoryProps {
  history: TicketEscalation[];
}

export function EscalationHistory({ history }: EscalationHistoryProps) {
  return (
    <div className="card">
      <h2>Escalation History</h2>

      {history.length === 0 ? (
        <p>No escalations recorded.</p>
      ) : (
        <ul>
          {history.map((escalation) => (
            <li key={escalation.id}>
              <strong>
                {escalation.previousPriority} → {escalation.newPriority}
              </strong>
              <p>{escalation.reason}</p>
              <small>{formatDateTime(escalation.escalatedAtUtc)}</small>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
