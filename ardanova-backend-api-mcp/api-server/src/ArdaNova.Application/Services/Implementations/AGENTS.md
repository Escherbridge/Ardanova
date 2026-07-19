# Opportunity role conversion

Project-wizard roles cross the opportunity boundary as typed allocation and
application-state fields. `OpportunityService` maps the open flag to the
existing lifecycle status and stores optional equity metadata in a versioned
compensation envelope, avoiding a schema-only field that older deployments
cannot read. API responses decode the envelope back into typed fields.
