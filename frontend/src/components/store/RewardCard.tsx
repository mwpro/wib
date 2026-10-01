import { useState, useRef, useEffect } from 'react'
import { Coins, MoreVertical, Edit2, Trash2, ChevronDown, ChevronUp, Package } from 'lucide-react'
import type { RewardItemResponse } from '../../types/store'

interface RewardCardProps {
  reward: RewardItemResponse
  walletBalance: number
  onPurchase: (reward: RewardItemResponse) => void
  onEdit: (reward: RewardItemResponse) => void
  onDelete: (reward: RewardItemResponse) => void
  isPurchasing?: boolean
}

export function RewardCard({
  reward,
  walletBalance,
  onPurchase,
  onEdit,
  onDelete,
  isPurchasing = false,
}: RewardCardProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [isExpanded, setIsExpanded] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  const canAfford = walletBalance >= reward.pointCost

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false)
      }
    }
    if (menuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [menuOpen])

  const descriptionLines = reward.description ? reward.description.split(/\r?\n/) : []
  const firstDescLine = descriptionLines[0] ?? ''
  const isExpandable =
    descriptionLines.length > 1 || (reward.description ? reward.description.length > 70 : false)

  return (
    <div
      data-testid="reward-card"
      className="group relative bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 shadow-[0_1px_3px_rgba(0,0,0,0.03)] hover:border-amber-300 dark:hover:border-amber-700/60 flex flex-col justify-between gap-3 transition-all"
    >
      <div className="flex flex-col gap-2">
        {/* Row 1: Title & Kebab Menu */}
        <div className="flex items-start justify-between gap-2">
          <h4 className="text-[15px] font-bold text-stone-900 dark:text-stone-50 leading-snug tracking-tight break-words flex-1 min-w-0">
            {reward.title}
          </h4>

          <div className="relative" ref={menuRef}>
            <button
              type="button"
              onClick={() => setMenuOpen(!menuOpen)}
              title="Opcje nagrody"
              className="p-1 rounded-lg text-stone-400 hover:text-stone-600 dark:hover:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 transition cursor-pointer"
            >
              <MoreVertical className="h-4 w-4" />
            </button>

            {menuOpen && (
              <div className="absolute right-0 top-7 z-30 w-36 bg-white dark:bg-[#1c1f2a] border border-stone-200 dark:border-stone-700 rounded-xl shadow-xl py-1 text-xs animate-in fade-in duration-100">
                <button
                  type="button"
                  onClick={() => {
                    setMenuOpen(false)
                    onEdit(reward)
                  }}
                  className="w-full px-3 py-2 text-left flex items-center gap-2 text-stone-700 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 cursor-pointer"
                >
                  <Edit2 className="h-3.5 w-3.5 text-stone-400" />
                  <span>Edytuj</span>
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setMenuOpen(false)
                    onDelete(reward)
                  }}
                  className="w-full px-3 py-2 text-left flex items-center gap-2 text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                >
                  <Trash2 className="h-3.5 w-3.5 text-rose-500" />
                  <span>Usuń</span>
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Row 2: Badges (Cost & Stock) */}
        <div className="flex items-center gap-2 flex-wrap">
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-amber-100/90 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300/80 dark:border-amber-800/80 text-xs font-bold tracking-tight shadow-2xs">
            <Coins className="h-3.5 w-3.5 text-amber-700 dark:text-amber-400" />
            <span>{reward.pointCost} pkt</span>
          </span>

          {reward.quantity != null && (
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/80 dark:border-stone-700/80 text-[11px] font-semibold">
              <Package className="h-3 w-3" />
              <span>Zostało: {reward.quantity} szt.</span>
            </span>
          )}
        </div>

        {/* Row 3: Description */}
        {reward.description && (
          <div className="text-xs text-stone-600 dark:text-stone-400 leading-relaxed mt-0.5">
            {isExpandable ? (
              <div>
                <p className={isExpanded ? 'whitespace-pre-line' : 'line-clamp-2'}>
                  {isExpanded ? reward.description : firstDescLine}
                </p>
                <button
                  type="button"
                  onClick={() => setIsExpanded(!isExpanded)}
                  className="inline-flex items-center gap-1 text-[11px] font-semibold text-amber-800 dark:text-amber-400 hover:underline mt-1 cursor-pointer"
                >
                  <span>{isExpanded ? 'Zwiń' : 'Rozwiń opis'}</span>
                  {isExpanded ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
                </button>
              </div>
            ) : (
              <p className="whitespace-pre-line">{reward.description}</p>
            )}
          </div>
        )}
      </div>

      {/* Row 4: Purchase CTA Button */}
      <div className="pt-2 border-t border-stone-100 dark:border-stone-800/80 mt-1">
        <button
          type="button"
          onClick={() => onPurchase(reward)}
          disabled={!canAfford || isPurchasing}
          title={!canAfford ? 'Niewystarczająca liczba punktów w portfelu' : undefined}
          className={`w-full py-2 px-3 rounded-xl text-xs font-bold transition flex items-center justify-center gap-1.5 select-none ${
            canAfford && !isPurchasing
              ? 'bg-amber-400 hover:bg-amber-500 active:scale-98 text-amber-950 border border-amber-500/40 shadow-xs cursor-pointer'
              : 'bg-stone-100 dark:bg-stone-800/80 text-stone-400 dark:text-stone-500 border border-stone-200/60 dark:border-stone-700/60 cursor-not-allowed opacity-60'
          }`}
        >
          <Coins className="h-3.5 w-3.5" />
          <span>Kup nagrodę</span>
        </button>
      </div>
    </div>
  )
}
