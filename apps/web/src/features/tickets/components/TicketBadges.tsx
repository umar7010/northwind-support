import { Badge, type BadgeTone } from '../../../components/Badge';
import { humanize } from '../../../lib/format';
import type { SlaStatus, TicketPriority, TicketStatus } from '../../../types/api';

const priorityTones: Record<TicketPriority, BadgeTone> = {
  Low: 'neutral',
  Medium: 'info',
  High: 'warning',
  Critical: 'danger',
};

const statusTones: Record<TicketStatus, BadgeTone> = {
  New: 'info',
  Open: 'info',
  InProgress: 'warning',
  Resolved: 'success',
  Closed: 'neutral',
};

export function PriorityBadge({ priority }: { priority: TicketPriority }) {
  return <Badge tone={priorityTones[priority]}>{priority}</Badge>;
}

export function StatusBadge({ status }: { status: TicketStatus }) {
  return <Badge tone={statusTones[status]}>{humanize(status)}</Badge>;
}


const slaTones: Record<SlaStatus, BadgeTone> = {
  NotApplicable: 'neutral',
  WithinSla: 'success',
  AtRisk: 'warning',
  Breached: 'danger',
  Met: 'success',
};

const slaLabels: Record<SlaStatus, string> = {
  NotApplicable: 'N/A',
  WithinSla: 'Within SLA',
  AtRisk: 'At Risk',
  Breached: 'SLA Breached',
  Met: 'SLA Met',
};

export function SlaBadge({ status }: { status: SlaStatus }) {
  return <Badge tone={slaTones[status]}>{slaLabels[status]}</Badge>;
}

