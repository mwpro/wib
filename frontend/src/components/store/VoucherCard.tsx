import { Ticket, Sparkles, Coins } from 'lucide-react'
import type { VoucherResponse } from '../../types/voucher'
import { formatRelativeTimePl } from '../../utils/relativeTime'

interface VoucherCardProps {
  voucher: VoucherResponse
  onRedeem: (voucher: VoucherResponse) => void
  isRedeeming?: boolean
}

export function VoucherCard({
  voucher,
  onRedeem,
  isRedeeming = false,
}: VoucherCardProps) {
  return (
    <div
      data-testid="voucher-card"
      className="group relative bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 border-l-4 border-l-amber-500 shadow-[0_1px_3px_rgba(0,0,0,0.03)] hover:border-amber-300 dark:hover:border-amber-700/60 flex flex-col sm:flex-row sm:items-center justify-between gap-3 transition-all"
    >
      <div className="flex items-start gap-3 min-w-0">
        <div className="w-10 h-10 rounded-xl bg-amber-100 dark:bg-amber-950/80 text-amber-900 dark:text-amber-300 border border-amber-300/80 dark:border-amber-800/80 flex items-center justify-center shrink-0">
          <Ticket className="w-5 h-5 text-amber-700 dark:text-amber-400" />
        </div>

        <div className="flex flex-col gap-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h4 className="text-[15px] font-bold text-stone-900 dark:text-stone-50 leading-snug tracking-tight break-words">
              {voucher.titleSnapshot}
            </h4>
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-amber-100/70 dark:bg-amber-950/50 text-amber-950 dark:text-amber-300 border border-amber-300/60 dark:border-amber-800/60 text-[11px] font-bold tracking-tight">
              <Coins className="h-3 w-3 text-amber-700 dark:text-amber-400" />
              <span>{voucher.pointCostSnapshot} pkt</span>
            </span>
          </div>

          <span className="text-xs text-stone-500 dark:text-stone-400">
            Kupiono {formatRelativeTimePl(voucher.purchasedAt)}
          </span>
        </div>
      </div>

      <div className="flex items-center sm:self-center shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0 border-stone-100 dark:border-stone-800">
        <button
          type="button"
          onClick={() => onRedeem(voucher)}
          disabled={isRedeeming}
          className="w-full sm:w-auto inline-flex items-center justify-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 active:scale-95 text-white rounded-xl text-xs font-bold shadow-xs transition cursor-pointer disabled:opacity-50"
        >
          <Sparkles className="h-3.5 w-3.5" />
          <span>{isRedeeming ? 'Realizowanie...' : 'Zrealizuj kupon'}</span>
        </button>
      </div>
    </div>
  )
}
