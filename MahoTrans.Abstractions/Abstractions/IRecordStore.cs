// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
/// Toolkit that implements RMS system.
/// </summary>
public interface IRecordStore : IToolkit
{
    /// <summary>
    /// Gets all existing stores.
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
    /// Notifies toolkit that store is closed.
    /// </summary>
    /// <param name="name">Store's name.</param>
    void CloseStore(string name);

    /// <summary>
    ///     Deletes record store. MIDP-side code must validate the operation before calling this method.
    /// </summary>
    /// <param name="name">Name of store.</param>
    /// <returns>False, if there is no such store.</returns>
    bool DeleteStore(string name);

    int AddRecord(string name, sbyte[] data, int offset, int count);

    void DeleteRecord(string name, int id);

    int GetSize(string name);

    int? GetSize(string name, int id);

    sbyte[]? GetRecord(string name, int id);

    void SetRecord(string name, int id, sbyte[] data, int offset, int count);

    int GetNextId(string name);

    int GetCount(string name);
}