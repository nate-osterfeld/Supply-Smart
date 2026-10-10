import { useState } from 'react';
import { 
  Folder, 
  Package, 
  MapPin, 
  SlidersHorizontal, 
  Plus, 
  ChevronRight, 
  ShieldCheck, 
  AlertTriangle,
  ClipboardList,
  ArrowLeft,
} from 'lucide-react';
import { useStorage } from '../../hooks/useStorage';

type ViewLevel = 'locations' | 'rooms' | 'containers';

export default function StoragePage() {
  const [viewLevel, setViewLevel] = useState<ViewLevel>('locations');
  const [selectedLocationId, setSelectedLocationId] = useState<number | null>(null);
  const [selectedRoomId, setSelectedRoomId] = useState<number | null>(null);
  
  // Hierarchy drill-down states for containers
  const [containerPathStack, setContainerPathStack] = useState<{ id: number; name: string }[]>([]);
  const currentParentContainerId = containerPathStack.length > 0 ? containerPathStack[containerPathStack.length - 1].id : null;
  const currentContainerName = containerPathStack.length > 0 ? containerPathStack[containerPathStack.length - 1].name : 'Select a container';

  // The actual container ID used to fetch items is the deepest selected container in the stack
  const selectedContainerId = currentParentContainerId ?? undefined;

  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  const { locations, isLoadingLocations, items, isLoadingItems } = useStorage(selectedContainerId);

  // Find currently selected entities for breadcrumbs & context
  const activeLocation = locations.find(l => l.id === selectedLocationId);
  const activeRoom = activeLocation?.rooms?.find(r => r.id === selectedRoomId);

  // Dynamic calculations based on live items in view
  const totalItemsCount = items.length;
  const safeItemsCount = items.filter(i => i.status === 'Safe').length;
  const healthPercentage = totalItemsCount > 0 ? Math.round((safeItemsCount / totalItemsCount) * 100) : 100;
  const isHealthGood = healthPercentage >= 80;
  const capacityPercentage = Math.min(100, totalItemsCount * 10);

  const summaryCards = [
    { 
      title: 'Location capacity', 
      value: `${capacityPercentage}%`, 
      detail: `${totalItemsCount} of 20 slots occupied`, 
      Icon: Folder, 
      showProgress: true,
      progressVal: capacityPercentage
    },
    { 
      title: 'Inventory health', 
      value: isHealthGood ? 'Good' : 'Needs Review', 
      detail: `${healthPercentage}% of items are within safe levels`, 
      Icon: isHealthGood ? ShieldCheck : AlertTriangle,
      iconColor: isHealthGood ? 'text-blue-600 bg-blue-50 dark:bg-blue-950/50 dark:text-blue-400' : 'text-amber-600 bg-amber-50 dark:bg-amber-950/50 dark:text-amber-400',
      showProgress: false 
    },
    { 
      title: 'Last cycle count', 
      value: 'Today', 
      detail: 'Counted by staff', 
      Icon: ClipboardList, 
      showProgress: false,
      iconColor: 'text-blue-600 bg-blue-50 dark:bg-blue-950/50 dark:text-blue-400'
    }
  ];

  // Helper containers calculation for footer display logic
  const allContainers = activeRoom?.containers || [];
  const displayedContainers = allContainers.filter(c => 
    currentParentContainerId === null 
      ? c.parentContainerId === null || c.parentContainerId === undefined
      : c.parentContainerId === currentParentContainerId
  );

  // Determine what list to show on the left panel based on drill-down level
  const renderLeftPanelContent = () => {
    if (isLoadingLocations) {
      return <p className="p-4 text-xs text-slate-400">Loading facility hierarchy...</p>;
    }

    return (
      <div>
        {/* Unified Step-by-Step Back Button */}
        {viewLevel !== 'locations' && (
          <button
            onClick={() => {
              if (viewLevel === 'containers') {
                if (containerPathStack.length > 0) {
                  setContainerPathStack(prev => prev.slice(0, prev.length - 1));
                } else {
                  setViewLevel('rooms');
                  setSelectedRoomId(null);
                }
              } else if (viewLevel === 'rooms') {
                setViewLevel('locations');
                setSelectedLocationId(null);
              }
            }}
            className="mb-3 flex w-full items-center gap-2 rounded-md bg-slate-100 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
          >
            <ArrowLeft className="size-3.5" /> 
                Back
          </button>
        )}

        {viewLevel === 'locations' && (
          locations.length === 0 ? <p className="p-4 text-xs text-slate-400">No locations found.</p> :
          locations.map((loc) => (
            <button 
              key={loc.id} 
              onClick={() => {
                setSelectedLocationId(loc.id);
                setViewLevel('rooms');
              }} 
              className="group mb-1 flex w-full items-start gap-3 rounded-lg border border-transparent px-3 py-3 text-left transition-colors hover:border-slate-200 hover:bg-slate-50 dark:hover:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <div className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                <Folder className="size-4" />
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate text-[13px] font-semibold text-slate-800 dark:text-slate-200">{loc.name}</p>
                  <ChevronRight className="size-4 text-slate-400" />
                </div>
                <p className="mt-1 truncate text-[11px] text-slate-400">{loc.rooms?.length || 0} rooms registered</p>
                <div className="mt-2 flex items-center gap-2">
                  <span className="text-[10px] font-medium uppercase tracking-wide text-slate-400">{loc.isPrimary ? 'Primary Facility' : 'Branch'}</span>
                </div>
              </div>
            </button>
          ))
        )}

        {viewLevel === 'rooms' && (
          (activeLocation?.rooms || []).length === 0 ? <p className="p-4 text-xs text-slate-400">No rooms found in this location.</p> :
          (activeLocation?.rooms || []).map((room) => (
            <button 
              key={room.id} 
              onClick={() => {
                setSelectedRoomId(room.id);
                setContainerPathStack([]); 
                setViewLevel('containers');
              }} 
              className="group mb-1 flex w-full items-start gap-3 rounded-lg border border-transparent px-3 py-3 text-left transition-colors hover:border-slate-200 hover:bg-slate-50 dark:hover:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <div className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                <Folder className="size-4" />
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate text-[13px] font-semibold text-slate-800 dark:text-slate-200">{room.name}</p>
                  <ChevronRight className="size-4 text-slate-400" />
                </div>
                <p className="mt-1 truncate text-[11px] text-slate-400">{room.containers?.length || 0} storage units inside</p>
                <div className="mt-2 flex items-center gap-2">
                  <span className="text-[10px] font-medium uppercase tracking-wide text-slate-400">Room</span>
                </div>
              </div>
            </button>
          ))
        )}

        {viewLevel === 'containers' && (
          displayedContainers.length === 0 ? (
            <p className="p-4 text-xs text-slate-400"></p>
          ) : (
            displayedContainers.map((container) => {
              const isSelected = selectedContainerId === container.id;
              const hasChildren = allContainers.some(c => c.parentContainerId === container.id);

              return (
                <button 
                  key={container.id} 
                  onClick={() => {
                    if (hasChildren) {
                        setContainerPathStack(prev => [...prev, { id: container.id, name: container.name }]);
                    } else {
                        setContainerPathStack(prev => [...prev, { id: container.id, name: container.name }]);
                    }
                    }}
                  className={`group mb-1 flex w-full items-start gap-3 rounded-lg border px-3 py-3 text-left transition-colors ${
                    isSelected 
                      ? 'border-blue-200 bg-blue-50/70 shadow-2xs dark:border-blue-900 dark:bg-blue-950/40' 
                      : 'border-transparent hover:border-slate-200 hover:bg-slate-50 dark:hover:border-slate-800 dark:hover:bg-slate-800/50'
                  }`}
                >
                  <div className={`mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md ${
                    isSelected ? 'bg-blue-100 text-blue-600 dark:bg-blue-900 dark:text-blue-300' : 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400'
                  }`}>
                    <Package className="size-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-2">
                      <p className={`truncate text-[13px] font-semibold ${isSelected ? 'text-blue-950 dark:text-blue-200' : 'text-slate-800 dark:text-slate-200'}`}>
                        {container.name}
                      </p>
                      <ChevronRight className="size-4 text-slate-400" />
                    </div>
                    <p className="mt-1 truncate text-[11px] text-slate-400">{activeRoom?.name} · {activeLocation?.name}</p>
                    <div className="mt-2 flex items-center gap-2">
                      <span className="text-[10px] font-medium uppercase tracking-wide text-slate-400">{container.kind}</span>
                      <span className="size-1 rounded-full bg-slate-300" />
                      <span className="text-[10px] text-slate-500">{hasChildren ? 'Drill down' : 'Storage Bin'}</span>
                    </div>
                  </div>
                </button>
              );
            })
          )
        )}
      </div>
    );
  };

  return (
    <div className="min-h-screen text-slate-900 pb-12 dark:text-slate-100">
      <main className="mx-auto max-w-[1400px]">
        {/* Header Section & Interactive Breadcrumbs */}
        <div className="mb-6 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <div className="mb-2 flex items-center gap-1.5 text-xs text-slate-400">
              <button 
                onClick={() => { setViewLevel('locations'); setSelectedLocationId(null); setSelectedRoomId(null); setContainerPathStack([]); }}
                className="hover:text-blue-600 transition-colors"
              >
                Locations
              </button>
              
              {activeLocation && (
                <>
                  <ChevronRight className="size-3 text-slate-300" />
                  <button 
                    onClick={() => { setViewLevel('rooms'); setSelectedRoomId(null); setContainerPathStack([]); }}
                    className="hover:text-blue-600 transition-colors font-medium text-slate-700 dark:text-slate-300"
                  >
                    {activeLocation.name}
                  </button>
                </>
              )}

              {activeRoom && (
                <>
                  <ChevronRight className="size-3 text-slate-300" />
                  <button 
                    onClick={() => { setViewLevel('containers'); setContainerPathStack([]); }}
                    className="hover:text-blue-600 transition-colors font-semibold text-blue-700 dark:text-blue-400"
                  >
                    {activeRoom.name}
                  </button>
                </>
              )}

              {containerPathStack.map((pathNode, idx) => (
                <span key={pathNode.id} className="flex items-center gap-1.5">
                  <ChevronRight className="size-3 text-slate-300" />
                  <button
                    onClick={() => {
                      setContainerPathStack(containerPathStack.slice(0, idx + 1));
                    }}
                    className={`hover:text-blue-600 transition-colors ${idx === containerPathStack.length - 1 ? 'font-semibold text-slate-900 dark:text-white' : 'text-slate-600'}`}
                  >
                    {pathNode.name}
                  </button>
                </span>
              ))}
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-950 dark:text-white">Storage Hierarchy</h1>
            <p className="mt-1 text-sm text-slate-500">Navigate your physical inventory by location and storage compartment.</p>
          </div>
          <div className="flex items-center gap-2">
            <button 
              onClick={() => setIsAddModalOpen(true)}
              className="flex h-9 items-center gap-2 rounded-lg bg-blue-600 px-3 text-xs font-semibold text-white shadow-2xs hover:bg-blue-700"
            >
              <Plus className="size-3.5" /> Add Room / Container
            </button>
          </div>
        </div>

        {/* Current Location Context Bar with Clickable Path */}
        <div className="mb-5 flex items-center justify-between rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-2xs dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-2 overflow-x-auto">
            <span className="flex shrink-0 items-center gap-2 text-xs font-semibold text-slate-700 dark:text-slate-300">
              <MapPin className="size-4 text-blue-600" /> Current path:
            </span>
            <span className="text-slate-300">|</span>
            
            <button 
              onClick={() => { setViewLevel('locations'); setSelectedLocationId(null); setSelectedRoomId(null); setContainerPathStack([]); }}
              className="text-xs hover:text-blue-600 text-slate-600 dark:text-slate-400 transition-colors"
            >
              Locations
            </button>

            {activeLocation && (
              <>
                <span className="text-slate-300">›</span>
                <button 
                  onClick={() => { setViewLevel('rooms'); setSelectedRoomId(null); setContainerPathStack([]); }}
                  className="text-xs hover:text-blue-600 font-semibold text-slate-700 dark:text-slate-300 transition-colors"
                >
                  {activeLocation.name}
                </button>
              </>
            )}

            {activeRoom && (
              <>
                <span className="text-slate-300">›</span>
                <button 
                  onClick={() => { setViewLevel('containers'); setContainerPathStack([]); }}
                  className="text-xs hover:text-blue-600 font-semibold text-slate-700 dark:text-slate-300 transition-colors"
                >
                  {activeRoom.name}
                </button>
              </>
            )}

            {containerPathStack.map((pathNode, idx) => (
              <span key={pathNode.id} className="flex items-center gap-1.5">
                <span className="text-slate-300">›</span>
                <span className={`text-xs ${idx === containerPathStack.length - 1 ? 'font-semibold text-blue-700 dark:text-blue-300' : 'text-slate-600'}`}>
                  {pathNode.name}
                </span>
              </span>
            ))}
          </div>
        </div>

        {/* Main Grid: Left Drill-Down Panel, Right Items Table */}
        <div className="grid gap-5 xl:grid-cols-[minmax(300px,0.83fr)_minmax(0,1.65fr)]">
          {/* Left Panel: Drill-Down List */}
          <section className="rounded-xl border border-slate-200 bg-white shadow-2xs dark:border-slate-800 dark:bg-slate-900">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 dark:border-slate-800">
              <div>
                <h2 className="text-sm font-bold text-slate-900 dark:text-white">
                  {viewLevel === 'locations' && 'Facility Locations'}
                  {viewLevel === 'rooms' && `Rooms in ${activeLocation?.name}`}
                  {viewLevel === 'containers' && `Storage Units in ${activeRoom?.name}`}
                </h2>
              </div>
              <SlidersHorizontal className="size-4 text-slate-400" />
            </div>
            
            <div className="p-3 max-h-[500px] overflow-y-auto">
              {renderLeftPanelContent()}
            </div>
            
            {/* Conditional Footer Text */}
            <div className="border-t border-slate-100 px-5 py-3 text-[11px] text-slate-400 dark:border-slate-800">
              {viewLevel === 'locations' ? 'Select a location to enter rooms' :
               viewLevel === 'rooms' ? 'Select a room to view storage units' :
               displayedContainers.length > 0 ? 'Select a storage container to view bin items' : ''}
            </div>
          </section>

          {/* Right Panel: Contents Table */}
          <section className="min-w-0 rounded-xl border border-slate-200 bg-white shadow-2xs dark:border-slate-800 dark:bg-slate-900">
            <div className="flex flex-col justify-between gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center dark:border-slate-800">
              <div>
                <div className="flex items-center gap-2">
                  <h2 className="text-sm font-bold text-slate-900 dark:text-white">
                    Contents in {currentContainerName}
                  </h2>
                  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-bold text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                    {items.length} items
                  </span>
                </div>
              </div>
              <button className="flex h-8 items-center gap-1.5 rounded-md border border-slate-200 px-2.5 text-xs font-semibold text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800">
                <Plus className="size-3.5" /> Add item
              </button>
            </div>

            <div className="overflow-x-auto">
              {isLoadingItems ? (
                <p className="p-8 text-xs text-slate-400">Loading bin contents...</p>
              ) : !selectedContainerId ? (
                <div className="flex h-[450px] w-full flex-col items-center justify-center text-center p-6">
                  <Package className="size-10 text-slate-300 mb-2" />
                  <p className="text-xs font-semibold text-slate-600 dark:text-slate-400">No container selected</p>
                </div>
              ) : items.length === 0 ? (
                <div className="flex h-[450px] w-full flex-col items-center justify-center text-center p-6">
                  <Package className="size-10 text-slate-300 mb-2" />
                  <p className="text-xs font-semibold text-slate-600 dark:text-slate-400">No items registered in this container</p>
                  <p className="mt-1 text-[11px] text-slate-400">Click 'Add item' to assign stock to this specific bin.</p>
                </div>
              ) : (
                <table className="w-full min-w-[720px] text-left">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/70 text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:border-slate-800 dark:bg-slate-800/40">
                      <th className="px-5 py-3">Item name</th>
                      <th className="px-3 py-3">SKU</th>
                      <th className="px-3 py-3">Lot number</th>
                      <th className="px-3 py-3">Expiration date</th>
                      <th className="px-3 py-3 text-right">Quantity</th>
                      <th className="px-3 py-3">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                    {items.map((item) => (
                      <tr key={item.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-800/30">
                        <td className="whitespace-nowrap px-5 py-3.5 text-[12px] font-semibold text-slate-800 dark:text-slate-200">{item.name}</td>
                        <td className="px-3 py-3.5 text-[11px] font-mono text-slate-500">{item.sku}</td>
                        <td className="px-3 py-3.5 text-[11px] text-slate-500">{item.lotNumber}</td>
                        <td className="px-3 py-3.5 text-[11px] text-slate-500">{item.expirationDate}</td>
                        <td className="px-3 py-3.5 text-right text-[12px] font-semibold text-slate-900 dark:text-white">{item.quantity}</td>
                        <td className="px-3 py-3.5">
                          <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold ${
                            item.status === 'Safe' ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300' :
                            item.status === 'Expiring Soon' ? 'bg-amber-50 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300' :
                            'bg-rose-50 text-rose-700 dark:bg-rose-950/50 dark:text-rose-300'
                          }`}>
                            <span className="size-1.5 rounded-full bg-current" />
                            {item.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>
        </div>

        {/* Dynamic Bottom Summary Cards */}
        <div className="mt-5 grid gap-5 md:grid-cols-3">
          {summaryCards.map((card) => {
            const IconComponent = card.Icon;
            return (
              <div key={card.title} className="rounded-xl border border-slate-200 bg-white p-4 shadow-2xs dark:border-slate-800 dark:bg-slate-900">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="text-xs font-semibold text-slate-500">{card.title}</p>
                    <p className="mt-1 text-xl font-bold tracking-tight text-slate-900 dark:text-white">{card.value}</p>
                  </div>
                  <div className={`rounded-lg p-2 ${card.iconColor || 'text-blue-600 bg-blue-50 dark:bg-blue-950/50 dark:text-blue-400'}`}>
                    <IconComponent className="size-4" />
                  </div>
                </div>
                {card.showProgress && (
                  <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                    <div className="h-full rounded-full bg-blue-500 transition-all duration-500" style={{ width: `${card.progressVal}%` }} />
                  </div>
                )}
                <p className="mt-3 text-[11px] text-slate-400">{card.detail}</p>
              </div>
            );
          })}
        </div>
      </main>

      {/* Add Modal */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-2xs">
          <div className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800 dark:bg-slate-900">
            <h3 className="text-lg font-bold text-slate-900 dark:text-white">Add New Storage Unit</h3>
            <p className="mt-1 text-xs text-slate-500">Configure a new room or bin in your facility hierarchy.</p>
            
            <div className="mt-4 space-y-3">
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Name / Identifier</label>
                <input placeholder="e.g., Bin #6 or Shelf 4" className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500 dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <button 
                onClick={() => setIsAddModalOpen(false)}
                className="h-9 rounded-md border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              >
                Cancel
              </button>
              <button 
                onClick={() => setIsAddModalOpen(false)}
                className="h-9 rounded-md bg-blue-600 px-4 text-xs font-semibold text-white hover:bg-blue-700"
              >
                Save Unit
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}