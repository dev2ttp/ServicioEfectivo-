namespace TotalPack.Efectivo.SSP
{
    /// <summary>
    /// Specifies the reasons for a note rejection.
    /// </summary>
    public enum RejectionReason
    {
        NoteAccepted,
        NoteLengthIncorrect,
        InvalidNote,
        ChannelInhabited,
        SecondNoteInsertedDuringRead,
        HostRejectedNote,
        InvalidNoteRead,
        NoteTooLong,
        ValidatorDisabled,
        MechanismSlowOrStalled,
        StrimmingAttempt,
        FraudChannelReject,
        NoNotesInserted,
        PeakDetectFail,
        TwistedNoteDetected,
        EscrowTimeout,
        BarcodeScanFail,
        IncorrectNoteWidth,
        NoteTooShort
    }
}
