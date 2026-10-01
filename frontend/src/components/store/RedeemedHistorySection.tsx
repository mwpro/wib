import { History, ChevronDown, ChevronUp } from 'lucide-react'
import { RedeemedVoucherCard } from './RedeemedVoucherCard'
import type { VoucherResponse } from '../../types/voucher'

interface RedeemedHistorySectionProps {
  redeemedVouchers: VoucherResponse[]
  isOpen: boolean
  onToggle: () => void
}

export function RedeemedHistorySection({
  redeemedVouchers,
  isOpen,
  onToggle,
}: RedeemedHistorySectionProps) {
  return (
    <section className="flex flex-col gap-2.5 pt-1">
      <button
        type="button"
        onClick={onToggle}
        className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800/60 transition cursor-pointer text-left"
      >
        <div className="flex items-center gap-2">
          <History className="h-4 w-4 text-stone-400" />
          <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
            Historia zrealizowanych
          </h3>
          <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/60 dark:border-stone-700/60">
            {redeemedVouchers.length}
          </span>
        </div>
        {isOpen ? (
          <ChevronUp className="h-4 w-4 text-stone-400" />
        ) : (
          <ChevronDown className="h-4 w-4 text-stone-400" />
        )}
      </button>

      {isOpen && (
        <div className="flex flex-col gap-2 animate-in fade-in duration-200">
          {redeemedVouchers.length > 0 ? (
            redeemedVouchers.map((voucher) => (
              <RedeemedVoucherCard key={voucher.id} voucher={voucher} />
            ))
          ) : (
            <div className="p-4 bg-white dark:bg-[#14161d] rounded-xl border border-stone-200/70 dark:border-stone-800/70 text-center text-xs text-stone-400 dark:text-stone-500">
              Brak zrealizowanych kuponów w historii.
            </div>
          )}
        </div>
      )}
    </section>
  )
}
