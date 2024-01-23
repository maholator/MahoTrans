// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that implements RMS system.
/// </summary>
public interface IRecordStore : IToolkit
{
    /// <summary>
    ///     Gets all existing stores.
    /// </summary>
    /// <returns>List of names of existing stores.</returns>
    string[] ListStores();

    /// <summary>
    ///     Checks can the store be opened.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="createIfNotExists"></param>
    /// <returns>
    ///     False if createIfNotExist=false and there is no such store. Jvm throws RecordStoreNotFoundException
    ///     in such case.
    /// </returns>
    bool OpenStore(string name, bool createIfNotExists);

    /// <summary>
    ///     Notifies toolkit that store is closed.
    /// </summary>
    /// <param name="name">Store's name.</param>
    void CloseStore(string name);

    /// <summary>
    ///     Deletes record store. MIDP-side code must validate the operation before calling this method.
    /// </summary>
    /// <param name="name">Name of store.</param>
    /// <returns>False, if there is no such store.</returns>
    bool DeleteStore(string name);

    /// <summary>
    ///     Adds data to record store. Data is added on the slot that returned by <see cref="GetNextId" />. See MIDP docs for
    ///     details.
    /// </summary>
    /// <param name="name">Name of store to add to.</param>
    /// <param name="data">Data to add.</param>
    /// <returns>Index of added slot.</returns>
    int AddRecord(string name, ReadOnlySpan<sbyte> data);


    /// <summary>
    ///     Deletes a slot from store.
    /// </summary>
    /// <param name="name">Name of the store.</param>
    /// <param name="id">Slot index.</param>
    /// <returns>
    ///     False is returned if <paramref name="id" /> is invalid. InvalidRecordIDException must be thrown by
    ///     implementation in such case.
    /// </returns>
    bool DeleteRecord(string name, int id);

    int GetSize(string name);

    /// <summary>
    ///     Gets size of slot in a store.
    /// </summary>
    /// <param name="name">Name of the store.</param>
    /// <param name="id">Slot index.</param>
    /// <returns>
    ///     Size of data in the slot in bytes. Null if <paramref name="id" /> is invalid. InvalidRecordIDException must be
    ///     thrown by implementation in such case.
    /// </returns>
    int? GetSize(string name, int id);

    sbyte[]? GetRecord(string name, int id);

    bool SetRecord(string name, int id, ReadOnlySpan<sbyte> data);

    int GetNextId(string name);

    int GetCount(string name);
}