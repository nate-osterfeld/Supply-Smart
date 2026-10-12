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
  ArrowRightLeft,
} from 'lucide-react';
import { useStorage } from '../../hooks/useStorage';

type ViewLevel = 'locations' | 'rooms' | 'containers';
type ModalType = 'none' | 'roomOrContainer' | 'item' | 'move';

export default function StoragePage() {
  const [viewLevel, setViewLevel] = useState<ViewLevel>('locations');
  const [selectedLocationId, setSelectedLocationId] = useState<number | null>(null);
  const [selectedRoomId, setSelectedRoomId] = useState<number | null>(null);
  
  // Hierarchy drill-down states for containers
  const [containerPathStack, setContainerPathStack] = useState<{ id: number; name: string }[]>([]);
  const currentParentContainerId = containerPathStack.length > 0 ? containerPathStack[containerPathStack.length - 1].id : null;
  const currentContainerName = containerPathStack.length > 0 ? containerPathStack[containerPathStack.length - 1].name : 'Select a container';

  const selectedContainerId = currentParentContainerId ?? undefined;

  // Modal State
  const [activeModal, setActiveModal] = useState<ModalType>('none');
  const [newEntityName, setNewEntityName] = useState('');
  const [newEntityKind, setNewEntityKind] = useState('Cabinet');

  // Item form state
  const [itemName, setItemName] = useState('');
  const [itemSku, setItemSku] = useState('');
  const [itemLot, setItemLot] = useState('');
  const [itemExp, setItemExp] = useState('');
  const [itemQty, setItemQty] = useState(10);
  const [itemMinThreshold, setItemMinThreshold] = useState<number | ''>(2);
  const [itemCost] = useState<number | ''>(5.00);
  const [itemType, setItemType] = useState<number>(0); // 0 = Consumable, 1 = Reusable

  // Move item state
  const [itemToMove, setItemToMove] = useState<{ id: number; name: string; quantity: number } | null>(null);
  const [targetContainerId, setTargetContainerId] = useState<number | ''>('');
  const [moveQty, setMoveQty] = useState<number | ''>('');
  const [moveThreshold, setMoveThreshold] = useState<number | ''>('');

  const { locations, isLoadingLocations, items, isLoadingItems, createRoom, createContainer, addItem, moveItem } = useStorage(selectedContainerId);

  const activeLocation = locations.find(l => l.id === selectedLocationId);
  const activeRoom = activeLocation?.rooms?.find(r => r.id === selectedRoomId);

  const handleSaveUnit = async () => {
    if (!newEntityName.trim()) return;
    try {
      if (viewLevel === 'rooms' && selectedLocationId) {
        await createRoom({ locationId: selectedLocationId, name: newEntityName });
      } else if (viewLevel === 'containers' && selectedRoomId) {
        await createContainer({ 
          roomId: selectedRoomId, 
          dto: { 
            name: newEntityName, 
            kind: newEntityKind, 
            parentContainerId: currentParentContainerId ?? null 
          } 
        });
      }
      setActiveModal('none');
      setNewEntityName('');
    } catch (error) {
      console.error('Failed to create unit:', error);
    }
  };

  const handleSaveItem = async () => {
    if (!selectedContainerId || !itemName.trim()) return;
    try {
      const payload: any = {
        name: itemName,
        sku: itemSku || 'SKU-GEN-01',
        lotNumber: itemLot || 'LOT-DEFAULT',
        quantity: Number(itemQty),
        globalThreshold: 50,
        localThreshold: itemMinThreshold === '' ? null : Number(itemMinThreshold),
        unitCost: Number(itemCost) || 0,
        type: Number(itemType)
      };

      // Only include expirationDate if consumable and date is provided
      if (Number(itemType) === 0 && itemExp) {
        payload.expirationDate = new Date(itemExp).toISOString();
      }

      await addItem({
        containerId: selectedContainerId,
        dto: payload
      });
      setActiveModal('none');
      setItemName(''); setItemSku(''); setItemLot(''); setItemExp(''); setItemQty(10);
    } catch (error) {
      console.error('Failed to add item:', error);
    }
  };

  const handleExecuteMove = async () => {
    if (!itemToMove || targetContainerId === '') return;
    try {
      await moveItem({
        itemId: itemToMove.id,
        targetContainerId: Number(targetContainerId),
        quantityToMove: moveQty === '' ? undefined : Number(moveQty),
        minimumThreshold: moveThreshold === '' ? undefined : Number(moveThreshold)
      });
      setActiveModal('none');
      setItemToMove(null);
      setTargetContainerId('');
      setMoveQty('');
      setMoveThreshold('');
    } catch (error) {
      console.error('Failed to relocate item:', error);
    }
  };

  // Calculations
  const totalItemsCount = items.length;
  const safeItemsCount = items.filter(i => i.status === 'Safe').length;
  const healthPercentage = totalItemsCount > 0 ? Math.round((safeItemsCount / totalItemsCount) * 100) : 100;
  const isHealthGood = healthPercentage >= 80;
  const capacityPercentage = Math.round((totalItemsCount / 20) * 100);
  // const capacityPercentage = Math.min(100, totalItemsCount * 10);

  const summaryCards = [
    { title: 'Location capacity', value: `${capacityPercentage}%`, detail: `${totalItemsCount} of 20 slots occupied`, Icon: Folder, showProgress: true, progressVal: capacityPercentage },
    { title: 'Inventory health', value: isHealthGood ? 'Good' : 'Needs Review', detail: `${healthPercentage}% of items within safe levels`, Icon: isHealthGood ? ShieldCheck : AlertTriangle, iconColor: isHealthGood ? 'text-blue-600 bg-blue-50 dark:bg-blue-950/50 dark:text-blue-400' : 'text-amber-600 bg-amber-50 dark:bg-amber-950/50 dark:text-amber-400', showProgress: false },
    { title: 'Last cycle count', value: 'Today', detail: 'Counted by staff', Icon: ClipboardList, showProgress: false, iconColor: 'text-blue-600 bg-blue-50 dark:bg-blue-950/50 dark:text-blue-400' }
  ];

  const allContainers = activeRoom?.containers || [];
  const displayedContainers = allContainers.filter(c => 
    currentParentContainerId === null 
      ? c.parentContainerId === null || c.parentContainerId === undefined
      : c.parentContainerId === currentParentContainerId
  );

  const allRoomContainers = activeLocation?.rooms.flatMap(r => r.containers) || [];

  const renderLeftPanelContent = () => {
    if (isLoadingLocations) return <p className="p-4 text-xs text-slate-400">Loading facility hierarchy...</p>;

    return (
      <div>
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
            <ArrowLeft className="size-3.5" /> Back
          </button>
        )}

        {viewLevel === 'locations' && locations.map((loc) => (
          <button key={loc.id} onClick={() => { setSelectedLocationId(loc.id); setViewLevel('rooms'); }} className="group mb-1 flex w-full items-start gap-3 rounded-lg border border-transparent px-3 py-3 text-left transition-colors hover:border-slate-200 hover:bg-slate-50 dark:hover:border-slate-800 dark:hover:bg-slate-800/50">
            <div className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400"><Folder className="size-4" /></div>
            <div className="min-w-0 flex-1">
              <div className="flex items-center justify-between gap-2">
                <p className="truncate text-[13px] font-semibold text-slate-800 dark:text-slate-200">{loc.name}</p>
                <ChevronRight className="size-4 text-slate-400" />
              </div>
              <p className="mt-1 truncate text-[11px] text-slate-400">{loc.rooms?.length || 0} rooms registered</p>
            </div>
          </button>
        ))}

        {viewLevel === 'rooms' && (activeLocation?.rooms || []).map((room) => (
          <button key={room.id} onClick={() => { setSelectedRoomId(room.id); setContainerPathStack([]); setViewLevel('containers'); }} className="group mb-1 flex w-full items-start gap-3 rounded-lg border border-transparent px-3 py-3 text-left transition-colors hover:border-slate-200 hover:bg-slate-50 dark:hover:border-slate-800 dark:hover:bg-slate-800/50">
            <div className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400"><Folder className="size-4" /></div>
            <div className="min-w-0 flex-1">
              <div className="flex items-center justify-between gap-2">
                <p className="truncate text-[13px] font-semibold text-slate-800 dark:text-slate-200">{room.name}</p>
                <ChevronRight className="size-4 text-slate-400" />
              </div>
              <p className="mt-1 truncate text-[11px] text-slate-400">{room.containers?.length || 0} storage units inside</p>
            </div>
          </button>
        ))}

        {viewLevel === 'containers' && (
          displayedContainers.length === 0 ? (
            <p className="p-4 text-xs text-slate-400">No sub-containers here. View item contents on the right.</p>
          ) : (
            displayedContainers.map((container) => {
              const isSelected = selectedContainerId === container.id;
              const hasChildren = allContainers.some(c => c.parentContainerId === container.id);
              return (
                <button key={container.id} onClick={() => setContainerPathStack(prev => [...prev, { id: container.id, name: container.name }])} className={`group mb-1 flex w-full items-start gap-3 rounded-lg border px-3 py-3 text-left transition-colors ${isSelected ? 'border-blue-200 bg-blue-50/70 shadow-2xs dark:border-blue-900 dark:bg-blue-950/40' : 'border-transparent hover:border-slate-200 hover:bg-slate-50 dark:hover:border-slate-800 dark:hover:bg-slate-800/50'}`}>
                  <div className={`mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md ${isSelected ? 'bg-blue-100 text-blue-600 dark:bg-blue-900 dark:text-blue-300' : 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400'}`}><Package className="size-4" /></div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-2">
                      <p className={`truncate text-[13px] font-semibold ${isSelected ? 'text-blue-950 dark:text-blue-200' : 'text-slate-800 dark:text-slate-200'}`}>{container.name}</p>
                      <ChevronRight className="size-4 text-slate-400" />
                    </div>
                    <div className="mt-2 flex items-center gap-2">
                      <span className="text-[10px] font-medium uppercase tracking-wide text-slate-400">{container.kind}</span>
                      <span className="size-1 rounded-full bg-slate-300" />
                      <span className="text-[10px] text-slate-500">{hasChildren ? 'Storage' : 'Bin'}</span>
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
        {/* Header & Breadcrumbs */}
        <div className="mb-6 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div className="w-full">
            <div className="mb-3 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-2xs dark:border-slate-800 dark:bg-slate-900">
              <MapPin className="size-4 text-slate-400 shrink-0" />
              <span className="text-xs font-semibold text-slate-700 dark:text-slate-300">Current location</span>
              <span className="text-slate-300 dark:text-slate-700">→</span>
              
              <div className="flex flex-wrap items-center gap-1.5 text-xs text-slate-500 overflow-x-auto">
                <button 
                  onClick={() => { setViewLevel('locations'); setSelectedLocationId(null); setSelectedRoomId(null); setContainerPathStack([]); }} 
                  className="hover:text-blue-600 transition-colors text-slate-600 dark:text-slate-400"
                >
                  Locations
                </button>
                {activeLocation && (
                  <>
                    <ChevronRight className="size-3 text-slate-300" />
                    <button 
                      onClick={() => { setViewLevel('rooms'); setSelectedRoomId(null); setContainerPathStack([]); }} 
                      className="hover:text-blue-600 transition-colors text-slate-600 dark:text-slate-400"
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
                      className="hover:text-blue-600 transition-colors text-slate-600 dark:text-slate-400"
                    >
                      {activeRoom.name}
                    </button>
                  </>
                )}
                {containerPathStack.map((pathNode, idx) => {
                  return (
                    <span key={pathNode.id} className="flex items-center gap-1.5">
                      <ChevronRight className="size-3 text-slate-300" />
                      <button 
                        onClick={() => setContainerPathStack(containerPathStack.slice(0, idx + 1))} 
                        className="hover:text-blue-600 transition-colors text-slate-600 dark:text-slate-400"
                      >
                        {pathNode.name}
                      </button>
                    </span>
                  );
                })}
              </div>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-950 dark:text-white">Storage Hierarchy</h1>
            <p className="mt-1 text-sm text-slate-500">Navigate your physical inventory by location and storage compartment.</p>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            {viewLevel === 'locations' ? (
              <span className="text-xs text-slate-400 italic">Select a location to add rooms</span>
            ) : (
              <button onClick={() => setActiveModal('roomOrContainer')} className="flex h-9 items-center gap-2 rounded-lg bg-blue-600 px-3 text-xs font-semibold text-white shadow-2xs hover:bg-blue-700">
                <Plus className="size-3.5" /> {viewLevel === 'rooms' ? 'Add Room' : 'Add Storage Unit'}
              </button>
            )}
          </div>
        </div>

        {/* Summary Metrics Cards */}
        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          {summaryCards.map((card, idx) => {
            const IconComponent = card.Icon;
            return (
              <div key={idx} className="rounded-xl border border-slate-200 bg-white p-4 shadow-2xs dark:border-slate-800 dark:bg-slate-900">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium text-slate-500 dark:text-slate-400">{card.title}</span>
                  <div className={`flex size-8 items-center justify-center rounded-lg ${card.iconColor || 'text-slate-600 bg-slate-100 dark:bg-slate-800 dark:text-slate-300'}`}>
                    <IconComponent className="size-4" />
                  </div>
                </div>
                <div className="mt-2 flex items-baseline gap-2">
                  <span className="text-xl font-bold text-slate-900 dark:text-white">{card.value}</span>
                </div>
                <p className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">{card.detail}</p>
                {card.showProgress && (
                  <div className="mt-3 h-1.5 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                    <div className="h-full bg-blue-600 rounded-full transition-all duration-500" style={{ width: `${card.progressVal}%` }} />
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Main Grid */}
        <div className="grid gap-5 xl:grid-cols-[minmax(300px,0.83fr)_minmax(0,1.65fr)]">
          <section className="rounded-xl border border-slate-200 bg-white shadow-2xs dark:border-slate-800 dark:bg-slate-900">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 dark:border-slate-800">
              <h2 className="text-sm font-bold text-slate-900 dark:text-white">
                {viewLevel === 'locations' && 'Facility Locations'}
                {viewLevel === 'rooms' && `Rooms in ${activeLocation?.name}`}
                {viewLevel === 'containers' && `Storage Units in ${activeRoom?.name}`}
              </h2>
              <SlidersHorizontal className="size-4 text-slate-400" />
            </div>
            <div className="p-3 max-h-[500px] overflow-y-auto">{renderLeftPanelContent()}</div>
          </section>

          <section className="min-w-0 rounded-xl border border-slate-200 bg-white shadow-2xs dark:border-slate-800 dark:bg-slate-900">
            <div className="flex flex-col justify-between gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center dark:border-slate-800">
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-slate-900 dark:text-white">Contents in {currentContainerName}</h2>
                <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-bold text-slate-600 dark:bg-slate-800 dark:text-slate-300">{items.length} items</span>
              </div>
              <button 
                disabled={!selectedContainerId} 
                onClick={() => setActiveModal('item')} 
                className="flex h-8 items-center gap-1.5 rounded-md border border-slate-200 px-2.5 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              >
                <Plus className="size-3.5" /> Add item
              </button>
            </div>

            <div className="overflow-x-auto">
              {isLoadingItems ? (
                <p className="p-8 text-xs text-slate-400">Loading bin contents...</p>
              ) : !selectedContainerId ? (
                <div className="flex h-[450px] w-full flex-col items-center justify-center text-center p-6">
                  <Package className="size-10 text-slate-300 mb-2" />
                  <p className="text-xs font-semibold text-slate-600 dark:text-slate-400">Select a storage container bin to view contents</p>
                </div>
              ) : items.length === 0 ? (
                <div className="flex h-[450px] w-full flex-col items-center justify-center text-center p-6">
                  <Package className="size-10 text-slate-300 mb-2" />
                  <p className="text-xs font-semibold text-slate-600 dark:text-slate-400">No items registered in this container</p>
                </div>
              ) : (
                <table className="w-full min-w-[720px] text-left">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/70 text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:border-slate-800 dark:bg-slate-800/40">
                      <th className="px-5 py-3">Item name</th>
                      <th className="px-3 py-3">SKU</th>
                      <th className="px-3 py-3">Lot number</th>
                      <th className="px-3 py-3">Expiration</th>
                      <th className="px-3 py-3 text-right">Quantity</th>
                      <th className="px-3 py-3">Status</th>
                      <th className="px-3 py-3 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                    {items.map((item: any) => (
                      <tr key={item.id} className="hover:bg-slate-50/50">
                        <td className="px-5 py-3.5 text-[12px] font-semibold text-slate-800 dark:text-slate-200">{item.name}</td>
                        <td className="px-3 py-3.5 text-[11px] font-mono text-slate-500">{item.sku}</td>
                        <td className="px-3 py-3.5 text-[11px] text-slate-500">{item.lotNumber}</td>
                        <td className="px-3 py-3.5 text-[11px] text-slate-500">
                          {item.type === 1 || !item.expirationDate ? 'N/A' : item.expirationDate}
                        </td>
                        <td className="px-3 py-3.5 text-right text-[12px] font-semibold text-slate-900 dark:text-white">{item.quantity}</td>
                        <td className="px-3 py-3.5">
                          <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold bg-emerald-50 text-emerald-700">
                            <span className="size-1.5 rounded-full bg-current" />{item.status}
                          </span>
                        </td>
                        <td className="px-3 py-3.5 text-right">
                          <button 
                            onClick={() => { setItemToMove({ id: item.id, name: item.name, quantity: item.quantity }); setActiveModal('move'); }}
                            className="inline-flex items-center gap-1 rounded border border-slate-200 px-2 py-1 text-[11px] font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
                          >
                            <ArrowRightLeft className="size-3" /> Move
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>
        </div>
      </main>

      {/* Modal: Add Room or Container / Sub-container */}
      {activeModal === 'roomOrContainer' && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-2xs">
          <div className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800 dark:bg-slate-900">
            <h3 className="text-lg font-bold text-slate-900 dark:text-white">
              {viewLevel === 'rooms' ? 'Add New Room' : 'Add Storage Unit / Bin'}
            </h3>
            <p className="mt-1 text-xs text-slate-500">
              {viewLevel === 'rooms' ? 'Configure a new room within this facility location.' : 'Configure a storage compartment or bin.'}
            </p>
            
            <div className="mt-4 space-y-3">
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Name</label>
                <input 
                  value={newEntityName} 
                  onChange={e => setNewEntityName(e.target.value)} 
                  placeholder={viewLevel === 'rooms' ? "e.g., Sterilization Room" : "e.g., Shelf 3 or Bin #4"} 
                  className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-blue-500 dark:border-slate-700 dark:bg-slate-800 dark:text-white" 
                />
              </div>
              {viewLevel === 'containers' && (
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Container Kind</label>
                  <select value={newEntityKind} onChange={e => setNewEntityKind(e.target.value)} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none bg-white dark:bg-slate-800 dark:border-slate-700 dark:text-white">
                    <option value="Cabinet">Cabinet</option>
                    <option value="Shelf">Shelf</option>
                    <option value="Bin">Bin</option>
                    <option value="Cart">Cart</option>
                  </select>
                </div>
              )}
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <button onClick={() => setActiveModal('none')} className="h-9 rounded-md border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300">Cancel</button>
              <button onClick={handleSaveUnit} className="h-9 rounded-md bg-blue-600 px-4 text-xs font-semibold text-white hover:bg-blue-700">Save</button>
            </div>
          </div>
        </div>
      )}

      {/* Modal: Add Item with Threshold & Type configuration */}
      {activeModal === 'item' && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-2xs">
          <div className="w-full max-w-lg rounded-xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800 dark:bg-slate-900">
            <h3 className="text-lg font-bold text-slate-900 dark:text-white">Add Inventory Item</h3>
            <p className="mt-1 text-xs text-slate-500">Assign stock and configure local par levels for this bin.</p>
            
            <div className="mt-4 grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Item Name</label>
                <input value={itemName} onChange={e => setItemName(e.target.value)} placeholder="e.g., Nitrile Gloves" className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">SKU</label>
                <input value={itemSku} onChange={e => setItemSku(e.target.value)} placeholder="GLV-LRG-01" className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Lot Number</label>
                <input value={itemLot} onChange={e => setItemLot(e.target.value)} placeholder="LOT-99210" className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Item Type</label>
                <select value={itemType} onChange={e => setItemType(Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none bg-white dark:bg-slate-800 dark:border-slate-700 dark:text-white">
                  <option value={0}>Consumable (Single-use)</option>
                  <option value={1}>Reusable (Asset/Tray)</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Initial Quantity</label>
                <input type="number" value={itemQty} onChange={e => setItemQty(Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Local Bin Minimum Threshold</label>
                <input type="number" value={itemMinThreshold} onChange={e => setItemMinThreshold(e.target.value === '' ? '' : Number(e.target.value))} placeholder="2" className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
              {Number(itemType) === 0 && (
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Expiration Date</label>
                  <input type="date" value={itemExp} onChange={e => setItemExp(e.target.value)} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
                </div>
              )}
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <button onClick={() => setActiveModal('none')} className="h-9 rounded-md border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300">Cancel</button>
              <button onClick={handleSaveItem} className="h-9 rounded-md bg-blue-600 px-4 text-xs font-semibold text-white hover:bg-blue-700">Save Item</button>
            </div>
          </div>
        </div>
      )}

      {/* Modal: Move Item between containers */}
      {activeModal === 'move' && itemToMove && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-2xs">
          <div className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800 dark:bg-slate-900">
            <h3 className="text-lg font-bold text-slate-900 dark:text-white">Relocate Item Batch</h3>
            <p className="mt-1 text-xs text-slate-500">Move "{itemToMove.name}" to another container or bin.</p>
            
            <div className="mt-4 space-y-3">
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Target Container / Bin</label>
                <select value={targetContainerId} onChange={e => setTargetContainerId(Number(e.target.value))} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none bg-white dark:bg-slate-800 dark:border-slate-700 dark:text-white">
                  <option value="">Select destination bin...</option>
                  {allRoomContainers.filter(c => c.id !== selectedContainerId).map(c => (
                    <option key={c.id} value={c.id}>{c.name} ({c.kind})</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Quantity to Move (Leave blank for full transfer)</label>
                <input type="number" value={moveQty} onChange={e => setMoveQty(e.target.value === '' ? '' : Number(e.target.value))} placeholder={`Max: ${itemToMove.quantity}`} className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">New Bin Minimum Threshold Override (Optional)</label>
                <input type="number" value={moveThreshold} onChange={e => setMoveThreshold(e.target.value === '' ? '' : Number(e.target.value))} placeholder="Overrides par level if new bin" className="mt-1 h-9 w-full rounded-md border border-slate-200 px-3 text-sm outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-white" />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <button onClick={() => setActiveModal('none')} className="h-9 rounded-md border border-slate-200 px-4 text-xs font-semibold text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300">Cancel</button>
              <button onClick={handleExecuteMove} className="h-9 rounded-md bg-blue-600 px-4 text-xs font-semibold text-white hover:bg-blue-700">Confirm Move</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}