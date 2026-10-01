import { Ticket, ChevronDown, ChevronUp } from 'lucide-react'
import { VoucherCard } from './VoucherCard'
import type { VoucherResponse } from '../../types/voucher'

interface WalletSectionProps {
  activeVouchers: VoucherResponse[]
  isOpen: boolean
  onToggle: () => void
  onRedeem: (voucher: VoucherResponse) => void
  redeemingVoucherId: number | null
}

export function WalletSection({
  activeVouchers,
  isOpen,
  onToggle,
  onRedeem,
  redeemingVoucherId,
}: WalletSectionProps) {
  return (
    <section className="flex flex-col gap-2.5">
      <button
        type="button"
        onClick={onToggle}
        className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800/60 transition cursor-pointer text-left"
      >
        <div className="flex items-center gap-2">
          <Ticket className="h-4 w-4 text-amber-600 dark:text-amber-400" />
          <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
            Mój portfel
          </h3>
          <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/60 dark:border-stone-700/60">
            {activeVouchers.length}
          </span>
        </div>
        {isOpen ? (
          <ChevronUp className="h-4 w-4 text-stone-400" />
        ) : (
          <ChevronDown className="h-4 w-4 text-stone-400" />
        )}
      </button>

      {isOpen && (
        <div className="flex flex-col gap-2.5 animate-in fade-in duration-200">
          {activeVouchers.length > 0 ? (
            activeVouchers.map((voucher) => (
              <VoucherCard
                key={voucher.id}
                voucher={voucher}
                onRedeem={onRedeem}
                isRedeeming={redeemingVoucherId === voucher.id}
              />
            ))
          ) : (
            <div className="p-5 bg-white dark:bg-[#14161d] rounded-2xl border border-stone-200/80 dark:border-stone-800/80 text-center flex flex-col items-center gap-1.5 shadow-[0_1px_3px_rgba(0,0,0,0.02)]">
              <div className="w-9 h-9 rounded-xl bg-stone-100 dark:bg-stone-800/80 text-stone-400 dark:text-stone-500 flex items-center justify-center">
                <Ticket className="h-4.5 w-4.5" />
              </div>
              <p className="text-xs font-bold text-stone-800 dark:text-stone-200">
                Twój portfel jest pusty
              </p>
              <p className="text-[11px] text-stone-500 dark:text-stone-400 max-w-xs">
                Brak aktywnych kuponów. Wymień zdobyte punkty na nagrodę w sklepie poniżej!
              </p>
            </div>
          )}
        </div>
      )}
    </section>
  )
}
