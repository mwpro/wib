import { ShoppingBag, Plus, ChevronDown, ChevronUp } from 'lucide-react'
import { RewardCard } from './RewardCard'
import type { RewardItemResponse } from '../../types/store'

interface CatalogSectionProps {
  rewards: RewardItemResponse[]
  isOpen: boolean
  onToggle: () => void
  walletBalance: number
  onPurchase: (reward: RewardItemResponse) => void
  onEdit: (reward: RewardItemResponse) => void
  onDelete: (reward: RewardItemResponse) => void
  onAddReward: () => void
  purchasingRewardId: number | null
}

export function CatalogSection({
  rewards,
  isOpen,
  onToggle,
  walletBalance,
  onPurchase,
  onEdit,
  onDelete,
  onAddReward,
  purchasingRewardId,
}: CatalogSectionProps) {
  return (
    <section className="flex flex-col gap-2.5 pt-1">
      <button
        type="button"
        onClick={onToggle}
        className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800/60 transition cursor-pointer text-left"
      >
        <div className="flex items-center gap-2">
          <ShoppingBag className="h-4 w-4 text-amber-600 dark:text-amber-400" />
          <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
            Dostępne nagrody
          </h3>
          <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/60 dark:border-stone-700/60">
            {rewards.length}
          </span>
        </div>
        {isOpen ? (
          <ChevronUp className="h-4 w-4 text-stone-400" />
        ) : (
          <ChevronDown className="h-4 w-4 text-stone-400" />
        )}
      </button>

      {isOpen && (
        <div className="animate-in fade-in duration-200">
          {rewards.length > 0 ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {rewards.map((reward) => (
                <RewardCard
                  key={reward.id}
                  reward={reward}
                  walletBalance={walletBalance}
                  onPurchase={onPurchase}
                  onEdit={onEdit}
                  onDelete={onDelete}
                  isPurchasing={purchasingRewardId === reward.id}
                />
              ))}
            </div>
          ) : (
            <div className="p-8 bg-white dark:bg-[#14161d] rounded-2xl border border-stone-200/90 dark:border-stone-800/80 text-center flex flex-col items-center gap-2.5 shadow-[0_2px_8px_rgba(0,0,0,0.02)]">
              <div className="w-12 h-12 rounded-2xl bg-amber-100 dark:bg-amber-950/80 text-amber-800 dark:text-amber-400 flex items-center justify-center border border-amber-300 dark:border-amber-800">
                <ShoppingBag className="h-6 w-6 text-amber-700 dark:text-amber-400" />
              </div>
              <h4 className="text-sm font-bold text-stone-900 dark:text-stone-100 tracking-tight">
                Brak nagród w sklepie
              </h4>
              <p className="text-xs text-stone-500 dark:text-stone-400 max-w-sm leading-relaxed">
                Dodaj pierwszą nagrodę lub przyjemność, na którą domownicy mogą wymieniać zdobyte punkty!
              </p>
              <button
                type="button"
                onClick={onAddReward}
                className="mt-1 inline-flex items-center gap-1.5 px-4 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/40 shadow-xs transition cursor-pointer"
              >
                <Plus className="h-4 w-4" />
                <span>Dodaj pierwszą nagrodę</span>
              </button>
            </div>
          )}
        </div>
      )}
    </section>
  )
}
